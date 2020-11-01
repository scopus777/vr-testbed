using System.Collections.Generic;
using UnityEngine;

public class VelocityCalculation : MonoBehaviour
{
    [Tooltip("The time window  which is considered in seconds.")]
    public float TimeWindow = 0.5f;
    
    // Returns the velocity calculated by summing up all position changes over the last frames in the TimeWindow
    public float TranslationVelocityAverage { get; private set; }
    
    // Returns the velocity calculated by taking the position of the hand from a frame determined by the TimeWindow
    public float TranslationVelocityFixed { get; private set; }
    
    // Returns the rotation velocity calculated by summing up all rotation changes over the last frames in the TimeWindow
    public float RotationVelocityAverage { get; private set; }
    
    private List<QueueEntry> queue;

    void Start()
    {
        TranslationVelocityAverage = 0;
        RotationVelocityAverage = 0;
        queue = new List<QueueEntry>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Remove entries outside the time window
        while (queue.Count > 0 && Time.realtimeSinceStartup - queue[0].time > TimeWindow)
            queue.RemoveAt(0);
        
        float distance = queue.Count > 0 ? Vector3.Distance(transform.position, queue[queue.Count - 1].position) : 0;
        float rotationDistance = queue.Count > 0 ? Vector3.Angle(transform.rotation * Vector3.forward, queue[queue.Count - 1].rotation * Vector3.forward) : 0;
        queue.Add(new QueueEntry(transform.position, transform.rotation, distance, rotationDistance));

        // calculate average translation velocity
        TranslationVelocityAverage = 0;
        foreach (QueueEntry entry in queue)
            TranslationVelocityAverage += entry.distance;
        TranslationVelocityAverage *= 1f / TimeWindow;
        
        // calculate fixed translation velocity
        TranslationVelocityFixed = Vector3.Distance(transform.position, queue[0].position) / TimeWindow;
        
        // calculate average rotation velocity
        RotationVelocityAverage = 0;
        foreach (QueueEntry entry in queue)
            RotationVelocityAverage += entry.rotationDistance;
        RotationVelocityAverage *= 1f / TimeWindow;
    }
    
    private struct QueueEntry
    {
        public float time;
        public Vector3 position;
        public Quaternion rotation;
        public float distance;
        public float rotationDistance;

        public QueueEntry(Vector3 position, Quaternion rotation, float distance, float rotationDistance)
        {
            this.time = Time.realtimeSinceStartup;
            this.position = position;
            this.rotation = rotation;
            this.distance = distance;
            this.rotationDistance = rotationDistance;
        }
    }
}
