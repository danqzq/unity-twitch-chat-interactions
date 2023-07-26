using System.Collections;
using UnityEngine;

namespace TwitchIntegration
{
    public abstract class TwitchMonoBehaviour : MonoBehaviour
    {
        protected virtual void Awake() => StartCoroutine(InitializeCoroutine());

        protected virtual void OnDestroy() => TwitchCommandManager.RemoveBehaviour(this);

        private IEnumerator InitializeCoroutine()
        {
            yield return new WaitUntil(() => TwitchManager.IsInitialized);
            yield return null;
            TwitchCommandManager.AddBehaviour(this);
        }
    }
}