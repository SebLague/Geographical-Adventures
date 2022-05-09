using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindUI : MonoBehaviour
{

	[HideInInspector] public int bindingIndex;
	public InputActionReference inputActionReference;

	[Header("UI fields")]
	[SerializeField]
	Button rebindButton;
	[SerializeField]
	TMP_Text rebindText;
	[SerializeField]
	Button resetButton;

	bool waitingForRebind;



	void Start()
	{

		rebindButton.onClick.AddListener(StartRebind);
		resetButton.onClick.AddListener(ResetBinding);

		RebindManager.Instance.rebindComplete += BindingCompletedOrCancelled;
		RebindManager.Instance.rebindCancelled += BindingCompletedOrCancelled;

		RebindManager.LoadBindingOverride(action);
		UpdateUI();
	}

	void OnEnable()
	{
		UpdateUI();
	}

	void OnDisable()
	{
		if (RebindManager.Instance != null)
		{
			RebindManager.Instance.Cancel();
		}
	}

	void StartRebind()
	{

		rebindText.text = "press any key";
		RebindManager.Instance.StartRebind(action, bindingIndex);
	}

	void BindingCompletedOrCancelled(InputAction action, int index)
	{
		if (this.action == action && this.bindingIndex == index)
		{
			UpdateUI();
		}
	}


	void UpdateUI()
	{
		rebindText.text = action.GetBindingDisplayString(bindingIndex);

	}

	void ResetBinding()
	{
		RebindManager.Instance.Cancel();
		RebindManager.ResetBinding(action, bindingIndex);
		UpdateUI();
	}

	string actionName
	{
		get
		{
			return inputActionReference.action.name;
		}
	}

	InputAction action
	{
		get
		{
			return inputActionReference.action;
		}
	}
}
