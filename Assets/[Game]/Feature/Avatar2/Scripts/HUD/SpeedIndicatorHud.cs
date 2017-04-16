using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedIndicatorHud : MonoBehaviour {

    public Image ascendingCursor;
    public Image glidingCursor;
    public Image descendingCursor;
    public Image divingCursor;
    public Image targetCursor;

    public Text targetText;
    public Text gaugeText;

    public Image containter;
    public Image gauge;

    public Avatar2.Character character;

    protected void Start()
    {
        var cfg = character.config;

        float minSpeed = cfg.ascendingTargetSpeed;
        float maxSpeed = cfg.divingTargetSpeed;

        float gauge_width = containter.rectTransform.rect.width;

        Vector3 position;

        // Ascending Cursor
        position = ascendingCursor.rectTransform.anchoredPosition;
        position.x = Mathf.InverseLerp(minSpeed, maxSpeed, cfg.ascendingTargetSpeed) * gauge_width;
        ascendingCursor.rectTransform.anchoredPosition = position;

        // Gliding Cursor
        position = glidingCursor.rectTransform.anchoredPosition;
        position.x = Mathf.InverseLerp(minSpeed, maxSpeed, cfg.glidingTargetSpeed) * gauge_width;
        glidingCursor.rectTransform.anchoredPosition = position;

        // Descending Cursor
        position = descendingCursor.rectTransform.anchoredPosition;
        position.x = Mathf.InverseLerp(minSpeed, maxSpeed, cfg.descendingTargetSpeed) * gauge_width;
        descendingCursor.rectTransform.anchoredPosition = position;

        // Diving Cursor
        position = divingCursor.rectTransform.anchoredPosition;
        position.x = Mathf.InverseLerp(minSpeed, maxSpeed, cfg.divingTargetSpeed) * gauge_width;
        divingCursor.rectTransform.anchoredPosition = position;

    }

    public void Update()
    {
        float speed = character.state.speed.get_value();
        float target_speed = character.GetTargetSpeed();

        float gauge_width = containter.rectTransform.rect.width;

        var cfg = character.config;
        float min_speed = cfg.ascendingTargetSpeed;
        float max_speed = cfg.divingTargetSpeed;

        float display_speed = Mathf.InverseLerp(min_speed, max_speed, speed);

        //Vector3 scale = gauge.rectTransform.localScale;
        //scale.x = display_speed;
        gauge.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, display_speed * 1024);// = (display_speed * gauge_width) - 1;

        // Target speed cursor Cursor
        Vector3 position;
        position = targetCursor.rectTransform.anchoredPosition;
        position.x = Mathf.Round(GetPositionOnGauge(min_speed, max_speed, target_speed, gauge_width));
        targetCursor.rectTransform.anchoredPosition = position;

        //Debug.Log(character.GetTargetSpeed());

        targetText.text = string.Format("{0:.00}", target_speed);
        gaugeText.text = string.Format("{0:.00}", speed);
    }

    public float GetPositionOnGauge(float min, float max, float value, float gauge_width)
    {
        return Mathf.InverseLerp(min, max, value) * gauge_width;
    }

}
