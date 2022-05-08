using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class InputManager : MonoBehaviour
{
    public static PlayerAction inputActions;

    public static event Action rebindComplete;
    public static event Action rebindCancelled;
    public static event Action<InputAction, int> rebindStarted;

    public static InputActionRebindingExtensions.RebindingOperation currentRebind;

    public static List<string> actionsChanged;

    private void Awake()
    {
        inputActions ??= new PlayerAction();
        actionsChanged = new List<string>();
    }


    public static void StartRebind(string actionName, int bindingIndex, TMP_Text statusText, bool excludeMouse)
    {
        InputAction action = inputActions.asset.FindAction(actionName);
        if (action is null || action.bindings.Count <= bindingIndex)
        {
            Debug.Log("Invalid action name or binding index");
            return;
        }

        if (action.bindings[bindingIndex].isComposite)
        {
            var firstPartIndex = bindingIndex + 1;
            if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isComposite)
                DoRebind(action, bindingIndex, statusText, true, excludeMouse);
        }
        else DoRebind(action, bindingIndex, statusText, false, excludeMouse);
    }

    private static void DoRebind(InputAction actionToRebind, int bindingIndex, TMP_Text statusText,
        bool allCompositeParts, bool excludeMouse)
    {
        currentRebind?.Cancel();
        currentRebind = null;
        
        if (actionToRebind is null || bindingIndex < 0) return;

        statusText.text = $"Press a {actionToRebind.expectedControlType} key...";
        actionToRebind.Disable();
        
        currentRebind = actionToRebind.PerformInteractiveRebinding(bindingIndex);
        
        currentRebind.WithCancelingThrough("<Keyboard>/"); // For some obscure reason, putting nothing or <Keyboard>/escape block the key e

        if (excludeMouse)
            currentRebind.WithControlsExcluding("Mouse");
        
        currentRebind.OnComplete(operation =>
        {
        
            actionToRebind.Enable();
            operation.Dispose();

            if (allCompositeParts)
            {
                var nextBindingIndex = bindingIndex + 1;

                if (nextBindingIndex < actionToRebind.bindings.Count &&
                    actionToRebind.bindings[nextBindingIndex].isComposite)
                    DoRebind(actionToRebind, nextBindingIndex, statusText, true, excludeMouse);
            } 
            
            Debug.Log($"Rebind complete for {actionToRebind.name}");
            actionsChanged.Add(actionToRebind.name);
            rebindComplete?.Invoke();
        });

        currentRebind.OnCancel(operation =>
        {
            actionToRebind.Enable();
            operation.Dispose();
            rebindCancelled?.Invoke();
            Debug.Log("Rebind cancelled");
        });

        rebindStarted?.Invoke(actionToRebind, bindingIndex);
        currentRebind.Start();
    }

    public static string GetBindingName(string actionName, int bindingIndex)
    {
        if (inputActions is null)
            inputActions = new PlayerAction();
            
        InputAction action = inputActions.asset.FindAction(actionName);
        return action.GetBindingDisplayString(bindingIndex);
    }

    public static void SaveChangedBindings()
    {
        if (actionsChanged.Count <= 0) return;
        
        foreach (var t in actionsChanged)
        {
            SaveBindingOverride(t);
        }

        actionsChanged.Clear();
    }

    public static void ReloadBindingsOnExit()
    {
        if (actionsChanged.Count <= 0) return;
        
        foreach (var t in actionsChanged)
        {
            Debug.Log(t);
            LoadBindingOverride(t);
        }

        actionsChanged.Clear();
    }
    
    public static void SaveBindingOverride(string actionName)
    {
        inputActions ??= new PlayerAction();    
                
        InputAction action = inputActions.asset.FindAction(actionName);
        
        for (int i = 0; i < action.bindings.Count; i++)
        {
            PlayerPrefs.SetString(action.actionMap + action.name + i, action.bindings[i].overridePath);
        }
    }

    public static void LoadBindingOverride(string actionName)
    {
        inputActions ??= new PlayerAction();    
                
        InputAction action = inputActions.asset.FindAction(actionName);

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if(!string.IsNullOrEmpty(PlayerPrefs.GetString(action.actionMap + action.name + i)))
                action.ApplyBindingOverride(i, PlayerPrefs.GetString(action.actionMap + action.name + i));
        }
    }

    public static void ResetBinding(string actionName, int bindingIndex)
    {
        InputAction action = inputActions.asset.FindAction(actionName);

        if(action == null || action.bindings.Count <= bindingIndex)
        {
            Debug.Log("Could not find action or binding");
            return;
        }

        if (action.bindings[bindingIndex].isComposite)
        {
            for (int i = bindingIndex; i < action.bindings.Count && action.bindings[i].isComposite; i++)
                action.RemoveBindingOverride(i);
        }
        else
            action.RemoveBindingOverride(bindingIndex);

        actionsChanged.Add(actionName);
    }
}