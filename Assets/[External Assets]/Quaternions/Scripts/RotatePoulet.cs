using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatePoulet : MonoBehaviour 
{
	Vector3 rotation;
	// Use this for initialization
	void Start () 
	{
		rotation = new Vector3 (Random.Range (0f, 360f), Random.Range (0f, 360f), Random.Range (0f, 360f));
	}
	
	// Update is called once per frame
	void Update () 
	{
		transform.Rotate (rotation * Time.deltaTime);
	}
}
