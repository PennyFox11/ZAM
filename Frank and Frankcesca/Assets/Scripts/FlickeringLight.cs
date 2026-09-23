using UnityEngine;
//Akhona Khoali
//https://youtu.be/4Q4BFykWOPY?si=IK892PRxobHCoaZL

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    private Light lightToFlicker;

    [Header("Intensity")]
    [SerializeField] private float minIntensity = 0.2f;
    [SerializeField] private float maxIntensity = 8f;

    [Header("Flicker Speed")]
    [SerializeField] private float timeBetweenIntensity = 0.05f;

    private float currentTimer;

    private void Awake()
    {
        lightToFlicker = GetComponent<Light>();

        if (minIntensity > maxIntensity)
        {
            (minIntensity, maxIntensity) = (maxIntensity, minIntensity);
        }
    }

    private void Update()
    {
        currentTimer += Time.deltaTime;

        if (currentTimer >= timeBetweenIntensity)
        {
            currentTimer = 0f;

            lightToFlicker.intensity =
                Random.Range(minIntensity, maxIntensity);
        }
    }
}