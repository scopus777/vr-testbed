using UnityEngine;

/// <summary>
/// Displays a indicator on the floor directly under the object.
/// </summary>
public class ShowPosition : MonoBehaviour
{
    public GameObject indicator;
    public LayerMask layer;
    
    [HideInInspector] public GameObject floor;
    [HideInInspector] public GameObject currentBall;
    
    void Awake()
    {
        currentBall = Instantiate(indicator);
        floor = TaskController.Instance.floor;
    }

    void Update()
    {
        bool hit = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, Mathf.Infinity,
                       layer) && hitInfo.collider.gameObject.Equals(floor);
        if (hit)
        {
            Vector3 test = hitInfo.point;
            // correct y position because otherwise in wim the position indicator will is not placed exactly on the floor because of slow physics
            test.y = hitInfo.collider.gameObject.transform.position.y +
                     hitInfo.collider.gameObject.transform.lossyScale.y / 2;
            currentBall.transform.position = test;
        }

        currentBall.gameObject.SetActive(hit);
    }

    private void OnDestroy()
    {
        Destroy(currentBall);
    }

    private void OnDisable()
    {
        if (currentBall)
            currentBall.SetActive(false);
    }

    private void OnEnable()
    {
        if (currentBall)
            currentBall.SetActive(true);
    }
}
