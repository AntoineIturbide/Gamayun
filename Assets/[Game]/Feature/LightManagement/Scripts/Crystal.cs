using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crystal : MonoBehaviour
{
	public float activatedEmissive;
	public float activatedLightIntensity;

	public float deactivatedEmissive;
	public float deactivatedLightIntensity;
	public float timeBeforeRespawn;

	public ColorManagement controller;

	Light crystalLight;
	Material mat;

	public bool isActivated;

	public GameObject activateParticle;
	public GameObject deactivateParticle;

	public Vector3 deactivatedScale;

	// Use this for initialization
	void Start ()
	{
		controller = transform.parent.GetComponent<ColorManagement>();

		mat = GetComponent<MeshRenderer>().material;
		mat.SetFloat("_Emissive", activatedEmissive);
		//Debug.Log("Emissive value : " + mat.GetFloat("_Emissive"));

		crystalLight = GetComponent<Light>();
		crystalLight.intensity = activatedLightIntensity;
	}

	public IEnumerator EmissiveSwitch (float transitionTime, float desiredEmissive)
	{
		float progress = 0f;
		float initialEmissive = mat.GetFloat("_Emissive");
		float emissiveAdj;

		while (progress < 1f)
		{
			emissiveAdj = Mathf.Lerp(initialEmissive, desiredEmissive, progress);

			
			progress += Time.deltaTime / transitionTime;
			yield return null;
		}
	}

	public IEnumerator LightSwitch (float transitionTime, float desiredLightIntensity)
	{
		float progress = 0f;
		float initialLightIntensity = crystalLight.intensity;

		while (progress < 1f)
		{
			crystalLight.intensity = Mathf.Lerp(initialLightIntensity, desiredLightIntensity, progress);

			progress += Time.deltaTime / transitionTime;
			yield return null;
		}
	}

	public IEnumerator DeActivation ()
	{
		StartCoroutine(EmissiveSwitch(1f, deactivatedEmissive));
		StartCoroutine(LightSwitch(1f, deactivatedLightIntensity));

		transform.localScale = deactivatedScale;

		isActivated = true;

		controller.CrystalCount();

		yield return new WaitForSeconds(timeBeforeRespawn);

		StartCoroutine(EmissiveSwitch(1f, activatedEmissive));
		StartCoroutine(LightSwitch(1f, activatedLightIntensity));
		Instantiate(activateParticle, transform.position, transform.rotation);

		yield return new WaitForSeconds(1f);

		transform.localScale = new Vector3 (1f,1f,1f);

		isActivated = false;
	}

	private void OnTriggerEnter (Collider other)
	{
		if(other.tag == "Player")
		{
			Instantiate(deactivateParticle, transform.position, Quaternion.identity);
			StopCoroutine(DeActivation());
			StartCoroutine(DeActivation());
		}
	}
}
