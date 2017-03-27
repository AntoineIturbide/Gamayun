using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodeOnCollide : MonoBehaviour
{
	public GameObject particleDestroy;

	private void OnTriggerEnter (Collider other)
	{
		if(particleDestroy != null)
		{
			Instantiate(particleDestroy, transform.position, Quaternion.identity);
		}
		Destroy(gameObject);
	}
}
