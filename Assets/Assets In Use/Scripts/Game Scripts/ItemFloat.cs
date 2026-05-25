using UnityEngine;

public class ItemFloat : MonoBehaviour
{
    [Header("Float Settings")]
    public float amplitude = 0.03f;  // Çok az — neredeyse fark edilmez
    public float frequency = 0.8f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        transform.position = startPos + new Vector3(0f, y, 0f);
    }
}