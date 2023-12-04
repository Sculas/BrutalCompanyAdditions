using System;
using System.Collections;
using UnityEngine;

namespace BrutalCompanyAdditions;

public class BCManager : MonoBehaviour {
    public static BCManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ExecuteAfterDelay(Action Action, float Delay) {
        StartCoroutine(DelayedExecution(Action, Delay));
    }

    private static IEnumerator DelayedExecution(Action Action, float Delay) {
        yield return new WaitForSeconds(Delay);
        Action.Invoke();
    }
}