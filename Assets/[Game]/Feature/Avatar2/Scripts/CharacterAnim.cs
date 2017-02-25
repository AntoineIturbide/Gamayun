using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Avatar2
{
	public class CharacterAnim : MonoBehaviour
	{
		public Character avatar;
		public Animator anim;

		public TrailRenderer leftTrail;
		public TrailRenderer rightTrail;
		public Colorful.LensDistortionBlur effects;

		public float minSpeedToTrail = 48f;
		public float minSpeedToBlur = 48f;

		public float distortionDamp = 512f;

		public float trailDamp = 350f;

		float speed;

		// Update is called once per frame
		void Update ()
		{
            //Debug.Log("Forward Speed : " + avatar.GetForwardSpeed());
            speed = avatar.state.thrust.get_value();
			anim.SetFloat("Speed", speed);
			TrailModif();
			if(effects != null) LensBlurModif();
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
                float time = Mathf.InverseLerp(avatar.config.minThrustSpeed, avatar.config.maxThrustSpeed, avatar.state.thrust.get_value());
                leftTrail.time = time;
                rightTrail.time = time;

            }
			else
			{
				leftTrail.time = 0f;
				rightTrail.time = 0f;
			}

		}
	}
}

