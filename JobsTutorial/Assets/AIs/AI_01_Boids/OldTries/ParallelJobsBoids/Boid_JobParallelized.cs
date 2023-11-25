using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace BoidsParallelJob
{
    public struct BoidData
    {
        public float3 Position;
        public float3 Velocity;
        public float3 Steering;
        public float3 Separation;
        public float3 Cohesion;
        public float3 Alignment;
    }

    public class Boid_JobParallelized : MonoBehaviour
    {
        public GameObject Target;
        public int NumBoids = 100;
        public float Velocidade = 1;
        public float Forcas = 1;
        public float Massa = 1;
        public float dMax = 5;
        public float dMin = 3;
        public float Vizinhanca = 10;
        public float PesoSeparacao = 1;
        public float PesoCoesao = 1;
        public float PesoAlinhamento = 1;
        public float RaioColisao = 5;
        public float RaioCenario = 5000;

        private NativeArray<BoidData> boids;

        public List<GameObject> BoidPrefabs;
        

        private void Start()
        {
            BoidPrefabs = new List<GameObject>();

            NumBoids = BoidPrefabs.Count;
            boids = new NativeArray<BoidData>(NumBoids, Allocator.Persistent);

            for (int i = 0; i < NumBoids; i++)
            {
                boids[i] = new BoidData
                {
                    Position = BoidPrefabs[i].transform.position,
                    Velocity = BoidPrefabs[i].transform.forward * Velocidade,
                    Steering = float3.zero,
                    Separation = float3.zero,
                    Cohesion = float3.zero,
                    Alignment = float3.zero
                };
            }
        }

        private void OnDestroy()
        {
            boids.Dispose();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            var job = new BoidJob
            {
                Target = Target.transform.position,
                Velocidade = Velocidade,
                Forcas = Forcas,
                Massa = Massa,
                dMax = dMax,
                dMin = dMin,
                Vizinhanca = Vizinhanca,
                PesoSeparacao = PesoSeparacao,
                PesoCoesao = PesoCoesao,
                PesoAlinhamento = PesoAlinhamento,
                RaioColisao = RaioColisao,
                RaioCenario = RaioCenario,
                Boids = boids,
                deltaTime = deltaTime,
                BoidPrefabs = BoidPrefabs // Pass reference to BoidPrefabs
            };

            var handle = job.Schedule(NumBoids, 64);
            handle.Complete();
        }
    }

    [BurstCompile]
    public struct BoidJob : IJobParallelFor
    {
        public float3 Target;
        public float Velocidade;
        public float Forcas;
        public float Massa;
        public float dMax;
        public float dMin;
        public float Vizinhanca;
        public float PesoSeparacao;
        public float PesoCoesao;
        public float PesoAlinhamento;
        public float RaioColisao;
        public float RaioCenario;
        public NativeArray<BoidData> Boids;
        public float deltaTime;
        public List<GameObject> BoidPrefabs;

        public void Execute(int i)
        {
            var boid = Boids[i];

            // MOVIMENTOS
            var desired = math.normalize((Target - boid.Position)) * Velocidade;
            var vel = boid.Velocity;
            var steering = desired - vel;

            // REGRAS DE BOIDS
            var separation = float3.zero;
            var cohesion = float3.zero;
            var alignment = float3.zero;
            var count = 0;
            for (int j = 0; j < Boids.Length; j++)
            {
                if (i != j)
                {
                    var other = Boids[j];
                    var d = math.distance(boid.Position, other.Position);
                    if (d < RaioColisao)
                    {
                        separation += boid.Position - other.Position;
                        count++;
                    }
                    if (d < Vizinhanca)
                    {
                        cohesion += other.Position;
                        alignment += other.Velocity;
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                separation /= count;
                cohesion /= count;
                alignment /= count;
            }

            var separationMagnitude = math.length(separation);
            var cohesionMagnitude = math.length(cohesion);
            var alignmentMagnitude = math.length(alignment);

            if (separationMagnitude > 0)
            {
                separation = math.normalize(separation) * Velocidade;
                steering += separation * PesoSeparacao;
            }

            if (cohesionMagnitude > 0)
            {
                cohesion = math.normalize(cohesion - boid.Position) * Velocidade;
                steering += cohesion * PesoCoesao;
            }

            if (alignmentMagnitude > 0)
            {
                alignment = math.normalize(alignment) * Velocidade;
                steering += alignment * PesoAlinhamento;
            }

            // LIMITES DO CENÁRIO
            var distanceFromCenter = math.distance(float3.zero, boid.Position);
            if (distanceFromCenter > RaioCenario)
            {
                steering += math.normalize(-boid.Position) * Velocidade;
            }

            // INTEGRAÇÃO
            steering = math.normalize(steering) * Forcas;
            var acceleration = steering / Massa;
            vel += acceleration * deltaTime;
            vel = math.normalize(vel) * Velocidade;
            boid.Position += vel * deltaTime;
            boid.Velocity = vel;

            Boids[i] = boid;

            // update boid data
            boid.Steering = steering;
            boid.Separation = separation;
            boid.Cohesion = cohesion;
            boid.Alignment = alignment;

            BoidPrefabs[i].transform.position = boid.Position;
            
            // move prefab based on updated position
            BoidPrefabs[i].GetComponent<BoidMovement>().MoveBoid(boid.Position);
        }

    }


}

