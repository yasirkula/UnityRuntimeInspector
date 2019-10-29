using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FCP_ExampleScript : MonoBehaviour
{
    public FlexibleColorPicker fcp;
    public Material material;

    private void Update() {
        material.color = fcp.color;
    }
}
