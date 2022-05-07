using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class RebindUI : MonoBehaviour
{
    [SerializeField]
    private InputActionReference inputActionReference;

    [SerializeField] private bool excludeMouse = true;
    
    [Range(0, 10)]
    [SerializeField]
    private int selectedBinding;    
    
    [SerializeField]
    private InputBinding.DisplayStringOptions displayStringOptions;
    
    [Header("Binding Info - DO NOT EDIT")]
    [SerializeField]
    private InputBinding inputBinding;

    private int bindingIndex;
    private string actionName;
    
    [Header("UI fields")]
    [SerializeField]
    private TMP_Text actionText;
    [SerializeField]
    private Button rebindButton;
    [SerializeField]
    private TMP_Text rebindText;
    [SerializeField] 
    private Button resetButton;

    private void OnEnable()
    {
        rebindButton.onClick.AddListener((() => DoRebind()));
        resetButton.onClick.AddListener((() => ResetBinding()));

        if (inputActionReference is not null)
        {
            InputManager.LoadBindingOverride(actionName);
            GetBindingInfo();
            UpdateUI();
        }
        
        InputManager.rebindComplete += UpdateUI;
        InputManager.rebindCancelled += UpdateUI;
    }

    private void OnDisable()
    {
        InputManager.rebindComplete -= UpdateUI;
        InputManager.rebindCancelled -= UpdateUI;
    }
    
    private void DoRebind()
    {
        InputManager.StartRebind(actionName, bindingIndex, rebindText, excludeMouse);
    }

    private void OnValidate()
    {
        if (inputActionReference is null) return;

        GetBindingInfo();
        UpdateUI();
    }

    private void GetBindingInfo()
    {
        if (inputActionReference.action is not null) actionName = inputActionReference.action.name;
        
        if(inputActionReference.action.bindings.Count > selectedBinding) inputBinding = inputActionReference.action.bindings[selectedBinding];
        bindingIndex = selectedBinding;
    }
    
    private void UpdateUI()
    {
        if (actionText is not null)
        {
            actionText.text = actionName;
            string actionTextAdditionnal = inputActionReference.action.bindings[selectedBinding].name;
            if (!String.IsNullOrEmpty(actionTextAdditionnal))
            {
                actionText.text += " - " + actionTextAdditionnal.Substring(0, 1).ToUpper() +
                                   actionTextAdditionnal.Substring(1).ToLower();
            }
        };
        if (rebindButton is not null)
        {
            if (Application.isPlaying)
            {
                rebindText.text = InputManager.GetBindingName(actionName, bindingIndex);
            }
            else
            {
                rebindText.text = inputActionReference.action.GetBindingDisplayString(bindingIndex);
            }
        }
    }

    private void ResetBinding()
    {
        InputManager.ResetBinding(actionName, bindingIndex);
        UpdateUI();
    }
}
