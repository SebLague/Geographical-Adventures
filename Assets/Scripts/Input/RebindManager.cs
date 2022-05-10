using UnityEngine;
using UnityEngine.InputSystem;

public class RebindManager : MonoBehaviour
{
	public PlayerAction activePlayerActions;

	// State
	InputActionRebindingExtensions.RebindingOperation activeRebindOperation;
	RebindUI activeRebindElement;

	RebindUI[] rebindElements;

	static RebindManager _instance;


	void Awake()
	{
		activePlayerActions = new PlayerAction();
		rebindElements = FindObjectsOfType<RebindUI>(includeInactive: true);

		SetActiveActionsToSavedValues();

		foreach (RebindUI element in rebindElements)
		{
			element.onRebindRequested += StartRebind;
		}
	}

	public void OnSettingsOpened()
	{
		// load saved bindings into UI
		foreach (RebindUI element in rebindElements)
		{
			LoadSavedBindingOverride(element.inputActionReference.action);
			element.UpdateUI();
		}
	}

	// Saves the bindings set in the UI to disc and applies them to the active input actions.
	public void SaveAndApplyBindings()
	{
		// Save bindings set in UI
		foreach (RebindUI element in rebindElements)
		{
			SaveBindingOverride(element.inputActionReference.action);
		}
		PlayerPrefs.Save();

		// Apply to active actions
		SetActiveActionsToSavedValues();

	}


	void SetActiveActionsToSavedValues()
	{
		foreach (var action in activePlayerActions)
		{
			LoadSavedBindingOverride(action);
		}
	}


	public void StartRebind(RebindUI element)
	{
		//	Cancel if trying to rebind same binding multiple times
		if (activeRebindElement == element)
		{
			Cancel();
			return;
		}

		Cancel();
		activeRebindElement = element;
		element.action.Disable();

		activeRebindOperation = element.action.PerformInteractiveRebinding(element.bindingIndex);
		activeRebindOperation.WithCancelingThrough("<Keyboard>/"); // For some obscure reason, putting nothing or <Keyboard>/escape block the key e
		activeRebindOperation.WithControlsExcluding("Mouse");

		activeRebindOperation.OnComplete(operation => OnRebindComplete());
		activeRebindOperation.OnCancel(operation => OnRebindCanceled());

		activeRebindOperation.Start();

	}

	void OnRebindComplete()
	{

		activeRebindElement.action.Enable();
		activeRebindOperation.Dispose();
		activeRebindElement.UpdateUI();
		activeRebindElement = null;

	}


	void SaveBindingOverride(InputAction action)
	{
		for (int i = 0; i < action.bindings.Count; i++)
		{
			PlayerPrefs.SetString(GetSaveID(action, i), action.bindings[i].overridePath);
		}
	}

	static void LoadSavedBindingOverride(InputAction action)
	{
		for (int i = 0; i < action.bindings.Count; i++)
		{
			string loadedValue = PlayerPrefs.GetString(GetSaveID(action, i));

			if (string.IsNullOrEmpty(loadedValue))
			{
				action.RemoveBindingOverride(i);
			}
			else
			{
				action.ApplyBindingOverride(i, loadedValue);
			}

		}
	}


	static string GetSaveID(InputAction action, int bindingIndex)
	{
		return $"{action.actionMap}_{action.name}_{bindingIndex}";
	}


	public void Cancel()
	{
		activeRebindOperation?.Cancel();
	}

	void OnRebindCanceled()
	{
		activeRebindElement.action.Enable();
		activeRebindOperation?.Dispose();
		activeRebindElement.UpdateUI();
		activeRebindElement = null;

	}

	public static RebindManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<RebindManager>(includeInactive: true);
			}
			return _instance;
		}
	}


}