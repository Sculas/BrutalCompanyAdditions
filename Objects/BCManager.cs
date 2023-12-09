using System;
using System.Collections;
using UnityEngine;

namespace BrutalCompanyAdditions.Objects;

public class BCManager : MonoBehaviour {
    public static BCManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Quick thanks to Venterok for this:
    // https://github.com/Venterok/HullBreakerCompany/blob/master/Hull/HullManager.cs#L35
    public void ExecuteAfterDelay(Action Action, float Delay) {
        StartCoroutine(DelayedExecution(Action, Delay));
    }

    private static IEnumerator DelayedExecution(Action Action, float Delay) {
        yield return new WaitForSeconds(Delay);
        Action.Invoke();
    }
}