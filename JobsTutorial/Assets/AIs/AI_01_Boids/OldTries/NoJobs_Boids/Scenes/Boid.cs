using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    public GameObject Target;
    private GameObject[] Boids;
    public float         Velocidade =1;
    public float         Forcas     =1;
    public float         Massa      =1;
    public float dMax = 5;
    public float dMin = 3;
    public float Vizinhanca = 10;
    public float PesoSeparacao     =1;
    public float PesoCoesao        =1;
    public float PesoAlinhamento   =1;
    public float RaioColisao       =5;
    public float RaioCenario      = 5000; 
    private void Start()
    {
        Boids = GameObject.FindGameObjectsWithTag("Boid");
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(this.transform.position, dMax);
        Gizmos.DrawSphere(this.transform.position, dMin);

        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawSphere(this.transform.position, Vizinhanca);

        Gizmos.color = Color.green;
        Debug.DrawLine(this.transform.position, this.transform.position + this.transform.forward * RaioCenario);
    }
    private void Update()
    {
        
        Vector3 Desired, Vel, Steering;
        Vector3 Separacao = Vector3.zero;
        Vector3 Cenario = Vector3.zero;

        #region MOVIMENTOS
        { 
            Desired = (Target.transform.position - this.transform.position).normalized * Velocidade;
            Vel = this.transform.forward * Velocidade;
            //  Vel = -this.transform.forward * Velocidade; //fugir
            Steering = Desired - Vel;
        }
        #endregion
        #region REGRAS DE BOIDS
        {
            //separação
            int Cont = 0;
            for (int i = 0; i < Boids.Length; i++)
            {
                if (Vector3.Distance(this.transform.position, Boids[i].transform.position) <= Vizinhanca)
                {
                    Cont++;
                    Separacao = Separacao + (this.transform.position - Boids[i].transform.position);
                }
            }
            if (Cont > 0)
            {
                Separacao = Separacao / Cont;
            }
        }
        #endregion
        #region CENÁRIO
        {//falta mays raycasts de validação de direção vetorial
            RaycastHit colisao;
            if (Physics.Raycast(this.transform.position, this.transform.forward, out colisao, RaioColisao))
            {
                if(colisao.collider.tag == "Cenario")
                {
                    Vector3 Normal = colisao.normal,
                            Tangente = Vector3.Cross(Normal, Vel).normalized * RaioCenario * 100;
                    Cenario += Tangente * (1.0f/colisao.distance);
                }
            }
            if (Physics.Raycast(this.transform.position, this.transform.forward + this.transform.right, out colisao, RaioColisao))
            {
                if (colisao.collider.tag == "Cenario")
                {
                    Vector3 Normal = colisao.normal,
                            Tangente = Vector3.Cross(Normal, Vel).normalized * RaioCenario * 100;
                    Cenario += Tangente * (1.0f / colisao.distance);
                }
            }
            if (Physics.Raycast(this.transform.position, this.transform.forward - this.transform.right, out colisao, RaioColisao))
            {
                if (colisao.collider.tag == "Cenario")
                {
                    Vector3 Normal = colisao.normal,
                            Tangente = Vector3.Cross(Normal, Vel).normalized * RaioCenario * 100;
                    Cenario += Tangente * (1.0f / colisao.distance);
                }
            }
        }
        #endregion
        #region CHEGAR LENTAMENTE
        {
            //ajusta os valores para um maximo de velocidade e forcas
            Steering = (Steering + Separacao * PesoSeparacao + Cenario) / Massa;
            Steering = Vector3.ClampMagnitude(Steering, Forcas);
            Vel += Steering;
            Vel = Vector3.ClampMagnitude(Vel, Velocidade);
            float Distancia = Vector3.Distance(Target.transform.position, this.transform.position);
            if (Distancia <= dMax)
            {
                Vel = Vel * (Distancia - dMin) / (dMax - dMin);
            }
        }
        #endregion
        #region MOVER O BOID
        {
            this.transform.LookAt(Target.transform.position);
            this.transform.position += Vel * Time.deltaTime;
        }
        #endregion
    } 
}
