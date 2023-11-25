using Unity.Mathematics;
using UnityEngine;

public class BoidMovement : MonoBehaviour
{
    public float Speed = 1;

    public void MoveBoid(float3 position)
    {
        transform.position = position;
    }

    private void Update()
    {
        transform.position += transform.forward * Speed * Time.deltaTime;
    }
}
