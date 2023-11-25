using UnityEngine;
using Unity.Mathematics;

namespace NewBoid_Sequential
{
    public class BoidBehaviour : MonoBehaviour
    {
        public FlockManager_Sequential flockManager;
        private Vector3 position;
        private Vector3 velocity;
        private Vector3 acceleration;

        private void Start()
        {
            position = transform.position;
            velocity = GetComponent<Rigidbody>().velocity;
        }

        private void Update()
        {
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(velocity);

            velocity = GetComponent<Rigidbody>().velocity;
            Flock();
        }
        
        private void Flock()
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int numNeighbors = 0;

            foreach (GameObject boidPrefab in flockManager.boidPrefabs)
            {
                if (boidPrefab == gameObject) continue;
                BoidBehaviour other = boidPrefab.GetComponent<BoidBehaviour>();
                Vector3 offset = other.position - position;
                float distance = offset.magnitude;
                if (distance < flockManager.separationDistance)
                {
                    separation -= offset.normalized / distance;
                }
                else if (distance < flockManager.alignmentDistance)
                {
                    alignment += other.velocity;
                    numNeighbors++;
                }
                else if (distance < flockManager.cohesionDistance)
                {
                    cohesion += other.position;
                    numNeighbors++;
                }
            }
            if (numNeighbors > 0)
            {
                alignment /= numNeighbors;
                cohesion /= numNeighbors;
                cohesion = (cohesion - position).normalized;
            }

            Vector3 boundsOffset = Vector3.zero;
            if (position.magnitude > flockManager.boundsRadius)
            {
                boundsOffset = -position.normalized * (position.magnitude - flockManager.boundsRadius);
            }

            separation = separation.normalized * flockManager.separationWeight;
            alignment = alignment.normalized * flockManager.alignmentWeight;
            cohesion = cohesion.normalized * flockManager.cohesionWeight;
            boundsOffset = boundsOffset.normalized;

            acceleration = separation + alignment + cohesion + boundsOffset;
            acceleration = Vector3.ClampMagnitude(acceleration, flockManager.maxForce);

            // add target following behavior
            Vector3 targetOffset = flockManager.target.position - position;
            float targetDistance = targetOffset.magnitude;

            if (targetDistance > 0.1f) // if the boid is far from the target
            {
                Vector3 targetVelocity = targetOffset.normalized * flockManager.maxSpeed;
                Vector3 targetAcceleration = (targetVelocity - velocity) * 10f; // use a high multiplier to make the boids follow the target more quickly
                acceleration += targetAcceleration;
            }

            velocity += acceleration * Time.deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, flockManager.maxSpeed);

            position += velocity * Time.deltaTime;
            acceleration = Vector3.zero;

        }
    }
    
}

