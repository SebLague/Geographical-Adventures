using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class RebindManager : MonoBehaviour
{
	public event Action<InputAction, int> rebindComplete;
	public event Action<InputAction, int> rebindCancelled;
	public event System.Action onBindingsSaved;

	// State
	List<InputAction> modifiedActions;
	InputActionRebindingExtensions.RebindingOperation activeRebindOperation;
	InputAction activeRebindAction;
	int activeRebindIndex;

	public RebindUI[] rebindElements;

	public static RebindManager _instance;
	public PlayerAction activePlayerActions;

	void Awake()
	{
		activePlayerActions = new PlayerAction();
		modifiedActions = new List<InputAction>();
		rebindElements = FindObjectsOfType<RebindUI>(includeInactive: true);

		SetActiveActionsToSavedValues();
	}

	public void OnSettingsOpened()
	{
		Debug.Log("Load");
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


	public void StartRebind(InputAction actionToRebind, int bindingIndex)
	{
		//	Cancel if trying to rebind same binding multiple times
		if (actionToRebind == activeRebindAction && activeRebindIndex == bindingIndex)
		{
			Cancel();
			return;
		}

		Cancel();
		activeRebindIndex = bindingIndex;
		activeRebindAction = actionToRebind;
		activeRebindAction.Disable();

		activeRebindOperation = actionToRebind.PerformInteractiveRebinding(bindingIndex);
		activeRebindOperation.WithCancelingThrough("<Keyboard>/"); // For some obscure reason, putting nothing or <Keyboard>/escape block the key e
		activeRebindOperation.WithControlsExcluding("Mouse");

		activeRebindOperation.OnComplete(operation => OnRebindComplete());
		activeRebindOperation.OnCancel(operation => OnRebindCanceled());

		activeRebindOperation.Start();

	}

	void OnRebindComplete()
	{
		if (activeRebindAction != null)
		{
			activeRebindAction.Enable();
			activeRebindOperation.Dispose();

			modifiedActions.Add(activeRebindAction);
			rebindComplete?.Invoke(activeRebindAction, activeRebindIndex);
			activeRebindAction = null;
		}
	}

	void OnRebindCanceled()
	{
		if (activeRebindAction != null)
		{
			activeRebindAction.Enable();
			activeRebindOperation?.Dispose();
			rebindCancelled?.Invoke(activeRebindAction, activeRebindIndex);
			activeRebindAction = null;
		}
	}

	/*

	public void SaveChangedBindings()
	{
		foreach (var t in modifiedActions)
		{
			SaveBindingOverride(t);
		}
		modifiedActions.Clear();

		PlayerPrefs.Save();
		onBindingsSaved?.Invoke();
	}
	*/

	void SaveBindingOverride(InputAction action)
	{
		for (int i = 0; i < action.bindings.Count; i++)
		{
			PlayerPrefs.SetString(GetSaveID(action, i), action.bindings[i].overridePath);
		}
	}

	/*
		public void ReloadBindingsOnExit()
		{
			foreach (var t in modifiedActions)
			{
				LoadSavedBindingOverride(t);
			}

			modifiedActions.Clear();

		}

	*/

	public static void LoadSavedBindingOverride(InputAction action)
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

	public static void ResetBinding(InputAction action, int bindingIndex)
	{

		if (action.bindings[bindingIndex].isComposite)
		{
			for (int i = bindingIndex; i < action.bindings.Count && action.bindings[i].isComposite; i++)
			{
				action.RemoveBindingOverride(i);
			}
		}
		else
		{
			action.RemoveBindingOverride(bindingIndex);
		}

		Instance.modifiedActions.Add(action);
	}

	public void Cancel()
	{
		activeRebindOperation?.Cancel();
		activeRebindAction = null;
		activeRebindOperation = null;
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