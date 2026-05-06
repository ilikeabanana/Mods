using UnityEngine;
using UnityEngine.Events;

	public class ExecuteOnSceneLoadRogue : MonoBehaviour
	{
		[Tooltip("Lower value ExecuteOnSceneLoad are executed first")]
		public int relativeExecutionOrder = 0;
		public UnityEvent onSceneLoad;

		public void Execute()
		{
			if (onSceneLoad == null)
				return;

			onSceneLoad.Invoke();
		}
	}