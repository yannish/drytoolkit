using System;
using UnityEditor;
using UnityEngine;

public class ColorContext : IDisposable
{
    private Color prevHandlesColor;
    private Color prevGizmosColor;
	private UnityEngine.Rendering.CompareFunction prevZTest;

	public ColorContext(Color color, UnityEngine.Rendering.CompareFunction? zTest = UnityEngine.Rendering.CompareFunction.Always)
    {
#if UNITY_EDITOR
        prevGizmosColor = Gizmos.color;
        prevHandlesColor = Handles.color;
		prevZTest = Handles.zTest;
        Handles.color = color;
        Gizmos.color = color;
		if (zTest != null)
			Handles.zTest = zTest.Value;
#endif
    }

    public void Dispose()
    {
#if UNITY_EDITOR
        Handles.color = prevHandlesColor;
        Gizmos.color = prevGizmosColor;
		Handles.zTest = prevZTest;
#endif
	}
}

