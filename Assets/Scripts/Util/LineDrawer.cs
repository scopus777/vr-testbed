using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private bool visible;

    public void Start()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
        visible = false;
    }

    private void Update()
    {
        // automatically disable ray in the next frame (DrawLineInGameView needs to be called to activate it again)
        lineRenderer.enabled = visible;
        visible = false;
    }

    //Draws lines through the provided vertices
    public void DrawLineInGameView(Vector3 start, Vector3 end, Color color)
    {
        lineRenderer.enabled = true;
        //Set color
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        //Set width
        lineRenderer.startWidth = 0.001f;
        lineRenderer.endWidth = 0.001f;

        //Set line count which is 2
        lineRenderer.positionCount = 2;

        //Set the position of both two lines
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        
        visible = true;
    }

    public void Destroy()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
}

