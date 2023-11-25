using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Profiling;

namespace NewBoid_JobParallelized
{
    public struct Boid
    {
        public float3 position;
        public float3 velocity;
        public float3 acceleration;
    }

    public class FlockManager : MonoBehaviour
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
        private JobHandle flockJobHandle;

        public GameObject[] boidPrefabs;
        public float spawnRadius;

        public int BatchSize = 64;
        public int ThreadLimitedTo = -1;

        void Start()
        {

            boids = new NativeArray<Boid>(numBoids, Allocator.Persistent);
            boidPrefabs = new GameObject[numBoids];


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

        void InitializeBoids()
        {
            for (int i = 0; i < boids.Length; i++)
            {
                Boid boid = new Boid
                {
                    position = boidPrefabs[i].transform.position,
                    velocity = boidPrefabs[i].GetComponent<Rigidbody>().velocity
                };
                boids[i] = boid;
            }
        }

        void Update()
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Create the flock job and schedule it
            var flockJob = new FlockJob
            {
                boids = boids,
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

            if (ThreadLimitedTo != -1) JobsUtility.JobWorkerCount = ThreadLimitedTo;
            Debug.Log("Threads: " + JobsUtility.JobWorkerCount);
            flockJobHandle = flockJob.Schedule(boids.Length, BatchSize);


            
        }


        void LateUpdate()
        {
            // Wait for the flock job to complete and update the boid positions
            flockJobHandle.Complete();
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
        }

        [BurstCompile]
        struct FlockJob : IJobParallelFor
        {
            public NativeArray<Boid> boids;
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

            public void Execute(int i)
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

                boids[i] = boid;
            }
        }
    }
}
