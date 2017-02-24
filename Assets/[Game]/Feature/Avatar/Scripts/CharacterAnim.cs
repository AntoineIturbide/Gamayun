using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Avatar
{
	public class CharacterAnim : MonoBehaviour
	{
		Character avatar;
		public Animator anim;

		public TrailRenderer leftTrail;
		public TrailRenderer rightTrail;
		public Colorful.LensDistortionBlur effects;

		public float minSpeedToTrail = 48f;
		public float minSpeedToBlur = 48f;

		public float distortionDamp = 512f;

		public float trailDamp = 350f;

		float speed;
		// Use this for initialization
		void Start ()
		{
			avatar = GetComponent<Character>();
		}

		// Update is called once per frame
		void Update ()
		{
			//Debug.Log("Forward Speed : " + avatar.GetForwardSpeed());
			speed = avatar.GetForwardSpeed();
			anim.SetFloat("Speed", speed);
			TrailModif();
			LensBlurModif ();
		}

		void LensBlurModif()
		{
			if (speed > minSpeedToBlur) 
			{
				effects.enabled = true;
				effects.Distortion = 1f + (speed / distortionDamp);
			} 
			else 
			{
				effects.enabled = false;
			}

		}

		void TrailModif()
		{
			if(speed > minSpeedToTrail)
			{
				leftTrail.time = avatar.GetForwardSpeed() / 350f;
				rightTrail.time = avatar.GetForwardSpeed() / 350f;
			}
			else
			{
				leftTrail.time = 0f;
				rightTrail.time = 0f;
			}

		}
	}
}

