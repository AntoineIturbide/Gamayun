using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeFreeze : MonoBehaviour
{
	public string freezeInput;
	public float slowedTimeScale = 0.1f;
	bool isSlowed = false;

	// Update is called once per frame
	void Update ()
	{
		if(Input.GetButtonDown(freezeInput))
		{
			if(isSlowed)
			{
				Time.timeScale = 1f;
				isSlowed = false;
			}
			else
			{
				isSlowed = true;
				Time.timeScale = slowedTimeScale;
			}
		}	
	}
}
