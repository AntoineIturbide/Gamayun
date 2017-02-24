
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorManagement : MonoBehaviour
{
	public Gradient dayColor;
	public Gradient nightColor;
	public ColorfulFog cameraFog;

	public Light sun;
	public float dayLightIntensity;
	public float nightLightIntensity;

	public float dayFogRange;
	public float nightFogRange;

	public float transitionTime;

	GradientColorKey[] dayColorsKeys;
	GradientColorKey[] nightColorsKeys;
	GradientColorKey[] transitionColorKeys;
	Gradient transitionGradient;

	Gradient colorSwitch;

	public Crystal[] levelCrystals = new Crystal[2];

	bool isSunny = false;

	// Use this for initialization
	void Start ()
	{
		dayColorsKeys = dayColor.colorKeys;
		nightColorsKeys = nightColor.colorKeys;
		transitionColorKeys = dayColor.colorKeys;
		transitionGradient = cameraFog.gradient;
	}

	public IEnumerator FogSwitch (float transitionTime, float desiredFogRange)
	{
		float progress = 0f;
		float initialFogRange = cameraFog.startDistance;

		while (progress < 1f)
		{
			cameraFog.startDistance = Mathf.Lerp(initialFogRange, desiredFogRange, progress);
			cameraFog.ApplyGradientChanges();

			progress += Time.deltaTime / transitionTime;

			yield return null;
		}
	}

	public IEnumerator LightSwitch(float transitionTime, float desiredLightIntensity)
	{
		float progress = 0f;
		float initialLightIntensity = sun.intensity;

		while (progress < 1f)
		{
			sun.intensity = Mathf.Lerp(initialLightIntensity, desiredLightIntensity, progress);
			progress += Time.deltaTime / transitionTime;
			yield return null;
		}
	}
	public IEnumerator ColorGradientSwitch (GradientColorKey[] fromGrad, GradientColorKey[] toGrad, float transitionTime)
	{
		float progress = 0f;

		while(progress < 1f)
		{
			for (int i = 0; i < dayColorsKeys.Length; i++)
			{
				transitionColorKeys[i].color = Color.Lerp(fromGrad[i].color, toGrad[i].color, progress);
			}
			progress += Time.deltaTime / transitionTime;

			transitionGradient.colorKeys = transitionColorKeys;
			cameraFog.gradient = transitionGradient;
			cameraFog.ApplyGradientChanges();
			//Debug.Log("Transition en cours");
			yield return null;
		}
		//Debug.Log("Transition terminée");
		yield return null;
	}

	// Update is called once per frame
	void Update ()
	{

		if (Input.GetKeyDown(KeyCode.A))
		{
			
			ToNightInstant();
		}

		if (Input.GetKeyDown(KeyCode.Z))
		{
			ToDayInstant();
		}

		if (Input.GetKeyDown(KeyCode.E))
		{
			StopCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, nightColorsKeys, transitionTime));
			StartCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, nightColorsKeys, transitionTime));

			StopCoroutine(LightSwitch(transitionTime, nightLightIntensity));
			StartCoroutine(LightSwitch(transitionTime, nightLightIntensity));
		}

		if(Input.GetKeyDown(KeyCode.R))
		{
			StopCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, dayColorsKeys, transitionTime));
			StartCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, dayColorsKeys, transitionTime));

			StopCoroutine(LightSwitch(transitionTime, dayLightIntensity));
			StartCoroutine(LightSwitch(transitionTime, dayLightIntensity));
		}

		//CrystalCount();
	}

	void ToNightInstant ()
	{
		StopCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, nightColorsKeys, transitionTime));
		cameraFog.gradient = nightColor;
		cameraFog.ApplyGradientChanges();
	}

	void ToDayInstant ()
	{
		StopCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, nightColorsKeys, transitionTime));
		cameraFog.gradient = dayColor;
		cameraFog.ApplyGradientChanges();
	}

	public void CrystalCount()
	{
		int activatedCrystals = 0;
		for(int i = 0; i< levelCrystals.Length; i++)
		{
			if(levelCrystals[i].isActivated)
			{
				activatedCrystals++;
			}
	//		Debug.Log("i = " + i + " activated crystals : " + activatedCrystals);
		}

		if(activatedCrystals == levelCrystals.Length)
		{
			StartCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, dayColorsKeys, transitionTime));
			StartCoroutine(LightSwitch(transitionTime, dayLightIntensity));
			StartCoroutine(FogSwitch(transitionTime, dayFogRange));
			isSunny = true;
		}
	}

	private void OnTriggerEnter (Collider other)
	{
		if(isSunny)
		{
			StartCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, dayColorsKeys, transitionTime));
			StartCoroutine(LightSwitch(transitionTime, dayLightIntensity));
			StartCoroutine(FogSwitch(transitionTime, dayFogRange));
		}
		else
		{
			StartCoroutine(ColorGradientSwitch(cameraFog.gradient.colorKeys, nightColorsKeys, transitionTime));
			StartCoroutine(LightSwitch(transitionTime, nightLightIntensity));
			StartCoroutine(FogSwitch(transitionTime, nightFogRange));
		}
	}
}
