using UnityEngine;

public class ParticleController : MonoBehaviour
{
    public Vector2 velocity;
    public float mass = 1f;

    
    public float minX = -5f;
    public float maxX = 5f;
    public float minY = -3f;
    public float maxY = 3f;
    

    void Start()
    {
        velocity = new Vector2(1f, 0.5f); 
    }

    void Update()
    {
        transform.position += (Vector3)velocity * Time.deltaTime;

        Vector3 currentPosition = transform.position;

        if (currentPosition.x < minX)
        {
            currentPosition.x = minX; 
            velocity.x *= -1;         
        }
        else if (currentPosition.x > maxX)
        {
            currentPosition.x = maxX; 
            velocity.x *= -1;         
        }


        if (currentPosition.y < minY)
        {
            currentPosition.y = minY; 
            velocity.y *= -1;         
        }
        else if (currentPosition.y > maxY)
        {
            currentPosition.y = maxY; 
            velocity.y *= -1;         
        }

        transform.position = currentPosition; 
    }
}