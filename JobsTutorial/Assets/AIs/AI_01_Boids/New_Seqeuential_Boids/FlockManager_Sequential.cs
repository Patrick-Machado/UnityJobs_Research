

using UnityEngine;
using System.Collections;

namespace NewBoid_Sequential
{
    public class FlockManager_Sequential : MonoBehaviour
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
        public GameObject[] boidPrefabs;
        public float spawnRadius;


        void Start()
        {


            boidPrefabs = new GameObject[numBoids];
            for (int i = 0; i < numBoids; i++)
            {
                // Instantiate the boid prefab and store the reference in boidPrefabs
                Vector3 position = new Vector3(UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius), UnityEngine.Random.Range(-spawnRadius, spawnRadius));
                Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range(-180, 180), UnityEngine.Random.Range(-180, 180), UnityEngine.Random.Range(-180, 180));
                GameObject _boidPrefab = Instantiate(boidPrefab, position, rotation);
                boidPrefabs[i] = _boidPrefab;
                _boidPrefab.GetComponent<BoidBehaviour>().flockManager = this;
            }



        }
    }
}






/*using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace NewBoid_Sequential
{
    public struct Boid
    {
        public float3 position;
        public float3 velocity;
        public float3 acceleration;
    }

    public class FlockManager_Sequential : MonoBehaviour
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

        public GameObject[] boidPrefabs;
        public float spawnRadius;

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

            // Perform the flocking behavior
            Flock();
        }

        void Flock()
        {
            for (int i = 0; i < numBoids; i++)
            {
                Boid boid = boids[i];
                float3 separation = float3.zero;
                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                int numNeighbors = 0;

                for (int j = 0; j < numBoids; j++)
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
                float3 targetOffset = new float3(target.position.x, target.position.y, target.position.z) - boid.position;
                float targetDistance = math.length(targetOffset);

                if (targetDistance > 0.1f) // if the boid is far from the target
                {
                    float3 targetVelocity = math.normalize(targetOffset) * maxSpeed;
                    float3 targetAcceleration = (targetVelocity - boid.velocity) * 10f; // use a high multiplier to make the boids follow the target more quickly
                    boid.acceleration += targetAcceleration;
                }

                boid.velocity += boid.acceleration * Time.deltaTime;
                boid.velocity = math.clamp(boid.velocity, -maxSpeed, maxSpeed);

                boid.position += boid.velocity * Time.deltaTime;
                boid.acceleration = float3.zero;
                boids[i] = boid;
            }
        }
    }
}*/