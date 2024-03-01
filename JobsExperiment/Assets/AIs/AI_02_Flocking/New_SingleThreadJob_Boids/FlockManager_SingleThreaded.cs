using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System.Collections;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Profiling;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Mathematics;
using System;
using System.Diagnostics;

namespace NewBoid_SingleThreadedJob
{
    public struct Boid
    {
        public float3 position;
        public float3 velocity;
        public float3 acceleration;
    }

    public class FlockManager_SingleThreaded : MonoBehaviour
    {
        public GameObject boidPrefab;
        public int numBoids = 50;
        public float maxSpeed = 5f;
        public float maxForce = 1f;
        public float separationDistance = 2f;
        public float alignmentDistance = 10f;
        public float cohesionDistance = 10f;
        public float separationWeight = 1f;
        public float alignmentWeight = 1f;
        public float cohesionWeight = 1f;
        public float boundsRadius = 50f;
        public Transform target;

        private NativeArray<Boid> boids;
        private NativeArray<Boid> updatedBoids;
        private JobHandle flockJobHandle;

        public GameObject[] boidPrefabs;
        public float spawnRadius;

        public int ThreadLimitedTo = -1;


        private int quantity;
        [SerializeField] float execution_time_seconds = 10;

        double currentTime = 0;

        ProfilerDataExporter m_profiler_data_exporter;
        public bool automatic_mode = true;

        private Stopwatch stopwatch = new Stopwatch();

        void Awake()
        {
            m_profiler_data_exporter = GetComponent<ProfilerDataExporter>();

            SpawnGraphy();
            m_profiler_data_exporter.Init();


            if (automatic_mode == false)
            {
                if (ThreadLimitedTo != -1) JobsUtility.JobWorkerCount = ThreadLimitedTo - 1;
                UnityEngine.Debug.Log("Manual-Mode Threads: " + (JobsUtility.JobWorkerCount + 1));
            }
            else
            {
                Update_Config();
                UnityEngine.Debug.Log("Auto-Mode Threads: " + (JobsUtility.JobWorkerCount + 1));
            }


            boids = new NativeArray<Boid>(numBoids, Allocator.Persistent);
            updatedBoids = new NativeArray<Boid>(numBoids, Allocator.Persistent);
            boidPrefabs = new GameObject[numBoids];

            quantity = PlayerPrefs.GetInt("quantity", (int)quantitySlider.value);
            agentsText.text = "Agents: " + quantity.ToString();
            quantitySlider.SetValueWithoutNotify(quantity);


            for (int i = 0; i < numBoids; i++)
            {
                // Instantiate the boid prefab and store the reference in boidPrefabs
                Vector3 position = new Vector3(UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius));
                Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range(-180, 180), UnityEngine.Random.Range(-180, 180), UnityEngine.Random.Range(-180, 180));
                GameObject _boidPrefab = Instantiate(boidPrefab, position, rotation);
                boidPrefabs[i] = _boidPrefab;
            }

            InitializeBoids();
        }

        public void Start()
        {
            stopwatch.Start();
        }
        void InitializeBoids()
        {
            for (int i = 0; i < boids.Length; i++)
            {
                Boid boid = new Boid
                {
                    position = boidPrefabs[i].transform.position,
                    velocity = boidPrefabs[i].GetComponent<Rigidbody>().velocity///
                };
                boids[i] = boid;
            }
        }

        int frameCount = 0; public int maxFrames = 100;

        void Update()
        {
            if (frameCount >= (maxFrames - 2)/*102f -2 = 100f)*/ )
            {
                pause_execution();
            }

            // Create the flock job and schedule it
            var flockJob = new FlockJob
            {
                boids = boids,
                updatedBoids = updatedBoids,
                maxSpeed = maxSpeed,
                maxForce = maxForce,
                separationDistance = separationDistance,
                alignmentDistance = alignmentDistance,
                cohesionDistance = cohesionDistance,
                separationWeight = separationWeight,
                alignmentWeight = alignmentWeight,
                cohesionWeight = cohesionWeight,
                boundsRadius = boundsRadius,
                targetPosition = target.position,
                deltaTime = Time.deltaTime
            };

            // Update the Boid data with the current position and velocity of the prefab
            for (int i = 0; i < boids.Length; i++)
            {
                GameObject boidPrefab = boidPrefabs[i];
                boidPrefab.transform.position = boids[i].position;
                boidPrefab.transform.rotation = Quaternion.LookRotation(boids[i].velocity);

                Boid boid = boids[i];
                boid.position = boidPrefab.transform.position;
                boid.velocity = boidPrefab.GetComponent<Rigidbody>().velocity;
                boids[i] = boid;
            }

            flockJobHandle = flockJob.Schedule();

            frameCount++;
        }
        void LateUpdate()
        {
            // Wait for the flock job to complete and update the boid positions
            flockJobHandle.Complete();

            // Swap the arrays
            var temp = boids;
            boids = updatedBoids;
            updatedBoids = temp;

            for (int i = 0; i < numBoids; i++)
            {
                Boid boid = boids[i];
                boid.velocity += boid.acceleration * Time.deltaTime;
                boid.velocity = math.clamp(boid.velocity, -maxSpeed, maxSpeed);

                // add target following behavior
                float3 targetOffset = new float3(target.position.x, target.position.y, target.position.z) - boid.position;

                float targetDistance = math.length(targetOffset);

                if (targetDistance > 0.1f) // if the boid is far from the target
                {
                    float3 targetVelocity = math.normalize(targetOffset) * maxSpeed;
                    float3 targetAcceleration = (targetVelocity - boid.velocity) * 10f; // use a high multiplier to make the boids follow the target more quickly
                    boid.acceleration += targetAcceleration;
                }

                boid.position += boid.velocity * Time.deltaTime;
                boid.acceleration = float3.zero;
                boidPrefabs[i].transform.position = boid.position;
                boidPrefabs[i].transform.rotation = Quaternion.LookRotation(boid.velocity);
                boids[i] = boid;
            }
        }

        void OnDestroy()
        {
            // Dispose of the boids array when the FlockManager is destroyed
            boids.Dispose();
            updatedBoids.Dispose();
        }

        [BurstCompile]
        struct FlockJob : IJob
        {
            [ReadOnly]
            public NativeArray<Boid> boids;
            public NativeArray<Boid> updatedBoids;
            public float maxSpeed;
            public float maxForce;
            public float separationDistance;
            public float alignmentDistance;
            public float cohesionDistance;
            public float separationWeight;
            public float alignmentWeight;
            public float cohesionWeight;
            public float boundsRadius;
            public float3 targetPosition;
            public float deltaTime;

            public void Execute()
            {
                for (int i = 0; i < boids.Length; i++)
                {
                    Boid boid = boids[i];
                    float3 separation = float3.zero;
                    float3 alignment = float3.zero;
                    float3 cohesion = float3.zero;
                    int numNeighbors = 0;

                    for (int j = 0; j < boids.Length; j++)
                    {
                        if (i == j) continue;
                        Boid other = boids[j];
                        float3 offset = other.position - boid.position;
                        float distance = math.length(offset);
                        if (distance < separationDistance)
                        {
                            separation -= math.normalize(offset) / distance;
                        }
                        else if (distance < alignmentDistance)
                        {
                            alignment += other.velocity;
                            numNeighbors++;
                        }
                        else if (distance < cohesionDistance)
                        {
                            cohesion += other.position;
                            numNeighbors++;
                        }
                    }

                    if (numNeighbors > 0)
                    {
                        alignment /= numNeighbors;
                        cohesion /= numNeighbors;
                        cohesion = math.normalize(cohesion - boid.position);
                    }

                    float3 boundsOffset = float3.zero;
                    if (math.length(boid.position) > boundsRadius)
                    {
                        boundsOffset = -math.normalize(boid.position) * (math.length(boid.position) - boundsRadius);
                    }

                    separation = math.normalize(separation) * separationWeight;
                    alignment = math.normalize(alignment) * alignmentWeight;
                    cohesion = math.normalize(cohesion) * cohesionWeight;
                    boundsOffset = math.normalize(boundsOffset);

                    boid.acceleration = separation + alignment + cohesion + boundsOffset;
                    boid.acceleration = math.clamp(boid.acceleration, -maxForce, maxForce);

                    // add target following behavior
                    float3 targetOffset = targetPosition - boid.position;
                    float targetDistance = math.length(targetOffset);

                    if (targetDistance > 0.1f) // if the boid is far from the target
                    {
                        float3 targetVelocity = math.normalize(targetOffset) * maxSpeed;
                        float3 targetAcceleration = (targetVelocity - boid.velocity) * 10f; // use a high multiplier to make the boids follow the target more quickly
                        boid.acceleration += targetAcceleration;
                    }

                    updatedBoids[i] = boid;
                }
            }
        }




        #region UI

        [Header("UI")]
        [SerializeField] private GameObject graphyPrefab = null;
        [SerializeField] private Slider quantitySlider = null;
        [SerializeField] private TextMeshProUGUI agentsText = null;

        private void SpawnGraphy()
        {
            GameObject graphy = Instantiate(graphyPrefab);
            m_profiler_data_exporter.graphy_Fps_ref = graphy.transform.GetChild(0).GetComponent<Tayx.Graphy.Fps.G_FpsText>();
        }

        public void OnRestartClicked()
        {
            PlayerPrefs.SetInt("quantity", (int)quantitySlider.value);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        #endregion
        void pause_execution()
        {
            stopwatch.Stop();
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            currentTime = elapsedMilliseconds;

            UnityEngine.Debug.Log("Finished Execution, Execution Time: " + currentTime + " Frames: " + maxFrames + " Media ET/F: " + (currentTime / maxFrames));

            string test_string_time = "Finished Execution, Execution Time: " + currentTime + " Frames: " + maxFrames + " Media ET/F: " + (currentTime / maxFrames);
            m_profiler_data_exporter.TestBreak(test_string_time, m_profiler_data_exporter, automatic_mode);
        }

        public void Update_Config()
        {
            int test_id = Test_Manager.instance.test_index;
            int amount_units; int amount_threads = 1; //int amount_frames;

            amount_units = int.Parse(Test_Manager.instance.GetTestInfo(test_id, Test_Manager.type_of_return.problem_size));
            ///amount_threads = int.Parse(Test_Manager.instance.GetTestInfo(test_id, Test_Manager.type_of_return.thread_count));

            // Update amount of Worker Threads
            JobsUtility.JobWorkerCount = amount_threads - 1;
            //Debug.Log("Threads: " + (JobsUtility.JobWorkerCount + 1));

            // Charge change
            numBoids = amount_units;

            // Update frames
            Test_Manager.type_of_return frames = Test_Manager.type_of_return.frames;
            maxFrames = int.Parse(Test_Manager.instance.GetTestInfo(test_id, frames));
        }
    }
}
