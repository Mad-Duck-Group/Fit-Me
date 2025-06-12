namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using EasyLayoutNS.Extensions;
	using UnityEngine;
	using UnityEngine.UI;

	/// <summary>
	/// Example scene script.
	/// </summary>
	public class EasyLayoutSampleScene : MonoBehaviour
	{
		/// <summary>
		/// Button prefab.
		/// </summary>
		public GameObject ButtonPrefab;

		/// <summary>
		/// Layout parameters.
		/// </summary>
		public GameObject LayoutParametres;

		GameObject LayoutTypeCompactOptions;

		GameObject LayoutTypeGridOptions;

		/// <summary>
		/// Button parameters.
		/// </summary>
		public GameObject ButtonParametres;

		/// <summary>
		/// Demo Layout.
		/// </summary>
		public GameObject DemoLayout;

		Dictionary<string,GameObject> views = new Dictionary<string,GameObject>();
		Dictionary<string,Button> viewsButtons = new Dictionary<string,Button>();

		EasyLayout currentLayout;
		Dictionary<string,Button> panelButtons = new Dictionary<string,Button>();
		Dictionary<string,Toggle> panelToggles = new Dictionary<string,Toggle>();
		Dictionary<string,InputField> panelInputs = new Dictionary<string,InputField>();

		Dictionary<string,Button> optionsButtons = new Dictionary<string,Button>();
		Dictionary<string,Toggle> optionsToggles = new Dictionary<string,Toggle>();
		Dictionary<string,InputField> optionsInputs = new Dictionary<string,InputField>();

		Dictionary<string,Anchors> anchorName2Enum = new Dictionary<string,Anchors>();
		Dictionary<string,HorizontalAligns> horizontalAlignName2Enum = new Dictionary<string,HorizontalAligns>();
		Dictionary<string,InnerAligns> innerAlignName2Enum = new Dictionary<string,InnerAligns>();

		Dictionary<string,Action<bool>> panelTogglesActions = new Dictionary<string,Action<bool>>();
		Dictionary<string,Action<float>> panelInputsActions = new Dictionary<string,Action<float>>();

		GameObject currentButton;

		void Start()
		{
			LayoutTypeCompactOptions = LayoutParametres.transform.Find("CompactOptions").gameObject;
			LayoutTypeGridOptions = LayoutParametres.transform.Find("GridOptions").gameObject;

			InitParametres();

			GenerateModesButtons();

			SetPlaygroundButtonsActions();
		}

		void SetPlaygroundButtonsActions()
		{
			foreach (Transform button in views["Playground"].transform.Find("Layout"))
			{
				var x = button;
				var buttonScript = button.gameObject.GetComponent<Button>();
				buttonScript.onClick.AddListener(() => SelectButton(x.gameObject));
			}
		}

		void InitParametres()
		{
			var anchors = (Anchors[])Enum.GetValues(typeof(Anchors));
			foreach (var anchor in anchors)
			{
				anchorName2Enum.Add(Enum2String(anchor), anchor);
			}

			var h_alings = (HorizontalAligns[])Enum.GetValues(typeof(HorizontalAligns));
			foreach (var h_align in h_alings)
			{
				horizontalAlignName2Enum.Add(Enum2String(h_align), h_align);
			}

			var i_aligns = (InnerAligns[])Enum.GetValues(typeof(InnerAligns));
			foreach (var i_align in i_aligns)
			{
				innerAlignName2Enum.Add(Enum2String(i_align), i_align);
			}

			// fill panel actions
			panelTogglesActions.Add("Top to Bottom", x => currentLayout.TopToBottom = x);
			panelTogglesActions.Add("Right to Left", x => currentLayout.RightToLeft = x);
			panelTogglesActions.Add("Skip Inactive", x => currentLayout.SkipInactive = x);

			panelTogglesActions.Add("Horizontal", x => {
				currentLayout.MainAxis = x ? Axis.Horizontal : Axis.Vertical;
			});
			panelTogglesActions.Add("Vertical", x => {
				currentLayout.MainAxis = !x ? Axis.Horizontal : Axis.Vertical;
			});

			panelTogglesActions.Add("Compact", x => {
				currentLayout.LayoutType = x ? LayoutTypes.Compact : LayoutTypes.Grid;
			});
			panelTogglesActions.Add("Grid", x => {
				currentLayout.LayoutType = !x ? LayoutTypes.Compact : LayoutTypes.Grid;
			});

			panelInputsActions.Add("Spacing.X", x => {
				var s = currentLayout.Spacing;
				s.x = x;
				currentLayout.Spacing = s;
			});
			panelInputsActions.Add("Spacing.Y", x => {
				var s = currentLayout.Spacing;
				s.y = x;
				currentLayout.Spacing = s;
			});
			panelInputsActions.Add("Margin.X", x => {
				var s = currentLayout.Margin;
				s.x = x;
				currentLayout.Margin = s;
			});
			panelInputsActions.Add("Margin.Y", x => {
				var s = currentLayout.Margin;
				s.y = x;
				currentLayout.Margin = s;
			});

			foreach (var button in LayoutParametres.GetComponentsInChildren<Button>(true))
			{
				panelButtons.Add(button.name, button);
			}

			foreach (var toggle in LayoutParametres.GetComponentsInChildren<Toggle>(true))
			{
				panelToggles.Add(toggle.name, toggle);
			}

			foreach (var input_field in LayoutParametres.GetComponentsInChildren<InputField>(true))
			{
				panelInputs.Add(input_field.name, input_field);
			}

			foreach (var button in panelButtons)
			{
				button.Value.onClick.AddListener(() => UpdatePanelParametres(button.Key));
			}

			foreach (var toggle in panelToggles)
			{
				toggle.Value.onValueChanged.AddListener(v => UpdatePanelParametres(toggle.Key, v));
			}

			foreach (var input_field in panelInputs)
			{
				input_field.Value.onEndEdit.AddListener(v => UpdatePanelParametres(input_field.Key, v));
			}

			foreach (var button in ButtonParametres.GetComponentsInChildren<Button>(true))
			{
				optionsButtons.Add(button.name, button);
			}

			foreach (var toggle in ButtonParametres.GetComponentsInChildren<Toggle>(true))
			{
				optionsToggles.Add(toggle.name, toggle);
			}

			foreach (var input_field in ButtonParametres.GetComponentsInChildren<InputField>(true))
			{
				optionsInputs.Add(input_field.name, input_field);
			}

			foreach (var button in optionsButtons)
			{
				button.Value.onClick.AddListener(() => UpdateButton(button.Key));
			}

			foreach (var toggle in optionsToggles)
			{
				toggle.Value.onValueChanged.AddListener(v => UpdateButton(toggle.Key, v));
			}

			foreach (var input_field in optionsInputs)
			{
				input_field.Value.onEndEdit.AddListener(v => UpdateButton(input_field.Key, v));
			}
		}

		void GenerateModesButtons()
		{
			foreach (Transform child in DemoLayout.transform)
			{
				var view = child.gameObject.name;

				var button = Instantiate(ButtonPrefab);
				button.transform.SetParent(transform, false);

				button.GetComponentInChildren<Text>().text = view;
				var buttonScript = button.GetComponent<Button>();

				buttonScript.onClick.AddListener(() => SetView(view));

				views.Add(view, child.gameObject);
				viewsButtons.Add(view, buttonScript);
			}

			GetComponent<EasyLayout>().UpdateLayout();

			SetView((new List<string>(views.Keys))[0]);
		}

		void SetView(string view)
		{
			foreach (var go in views.Values)
			{
				go.SetActive(false);
			}
			
			views[view].SetActive(true);

			foreach (var button in viewsButtons.Values)
			{
				button.interactable = true;
			}

			viewsButtons[view].interactable = false;

			ButtonParametres.SetActive(view=="Playground");

			currentLayout = views[view].GetComponentInChildren<EasyLayout>();

			LoadPanelParameters();
		}

		void LoadPanelParameters()
		{
			foreach (var button in panelButtons)
			{
				button.Value.interactable = true;
			}

			panelButtons[Enum2String(currentLayout.GroupPosition)].interactable = false;

			panelToggles[Enum2String(currentLayout.MainAxis)].isOn = true;
			panelToggles[Enum2String(currentLayout.LayoutType)].isOn = true;

			if (currentLayout.LayoutType==LayoutTypes.Compact)
			{
				LayoutTypeCompactOptions.SetActive(true);
				LayoutTypeGridOptions.SetActive(false);
			}
			else
			{
				LayoutTypeCompactOptions.SetActive(false);
				LayoutTypeGridOptions.SetActive(true);
			}

			panelButtons[Enum2String(currentLayout.RowAlign)].interactable = false;
			panelButtons[Enum2String(currentLayout.InnerAlign)].interactable = false;
			panelButtons["Grid" + Enum2String(currentLayout.CellAlign)].interactable = false;

			panelInputs["Spacing.X"].text = currentLayout.Spacing.x.ToString();
			panelInputs["Spacing.Y"].text = currentLayout.Spacing.y.ToString();
			panelInputs["Margin.X"].text = currentLayout.Margin.x.ToString();
			panelInputs["Margin.Y"].text = currentLayout.Margin.y.ToString();

			panelToggles["Top to Bottom"].isOn = currentLayout.TopToBottom;
			panelToggles["Right to Left"].isOn = currentLayout.RightToLeft;
			panelToggles["Skip Inactive"].isOn = currentLayout.SkipInactive;

			var rect = currentLayout.GetComponent<RectTransform>().rect;
			ButtonParametres.transform.Find("Panel.Width").GetComponent<InputField>().text = rect.width.ToString();
			ButtonParametres.transform.Find("Panel.Height").GetComponent<InputField>().text = rect.height.ToString();

			LayoutParametres.GetComponent<EasyLayout>().UpdateLayout();
		}

		void UpdatePanelParametres(string uiName)
		{
			if (anchorName2Enum.ContainsKey(uiName))
			{
				currentLayout.GroupPosition = anchorName2Enum[uiName];

				currentLayout.UpdateLayout();
				LoadPanelParameters();
				return ;
			}

			if (uiName.StartsWith("Grid", StringComparison.Ordinal))
			{
				uiName = uiName.Substring("Grid".Length);
				if (anchorName2Enum.ContainsKey(uiName))
				{
					currentLayout.CellAlign = anchorName2Enum[uiName];

					currentLayout.UpdateLayout();
					LoadPanelParameters();
					return ;
				}
			}

			if (horizontalAlignName2Enum.ContainsKey(uiName))
			{
				currentLayout.RowAlign = horizontalAlignName2Enum[uiName];

				currentLayout.UpdateLayout();
				LoadPanelParameters();
				return ;
			}

			if (innerAlignName2Enum.ContainsKey(uiName))
			{
				currentLayout.InnerAlign = innerAlignName2Enum[uiName];

				currentLayout.UpdateLayout();
				LoadPanelParameters();
				return ;
			}
		}

		void UpdatePanelParametres(string uiName, string value)
		{
			if (panelInputsActions.ContainsKey(uiName))
			{
				panelInputsActions[uiName](float.Parse(value));

				currentLayout.UpdateLayout();
				LoadPanelParameters();
				return ;
			}
		}

		void UpdatePanelParametres(string uiName, bool value)
		{
			if (panelTogglesActions.ContainsKey(uiName))
			{
				panelTogglesActions[uiName](value);

				currentLayout.UpdateLayout();
				LoadPanelParameters();
				return ;
			}
		}

		void UpdateButton(string uiName)
		{
			if (uiName == "Add Button")
			{
				var text = ButtonParametres.transform.Find("ButtonText").GetComponent<InputField>().text;

				var button = Instantiate(ButtonPrefab) as GameObject;
				button.transform.SetParent(currentLayout.transform, false);

				button.name = text;
				button.GetComponentInChildren<Text>().text = (text!="") ? text : "No-name";

				var buttonScript = button.GetComponent<Button>();
				buttonScript.onClick.AddListener(() => SelectButton(button));

				currentLayout.UpdateLayout();
			}

			if (uiName == "All Active")
			{
				foreach (Transform child in currentLayout.gameObject.transform)
				{
					child.gameObject.SetActive(true);
				}

				currentLayout.UpdateLayout();
			}

			if (uiName == "Destroy")
			{
				if (currentButton!=null)
				{
					Destroy(currentButton);
				}

				currentLayout.UpdateLayout();
			}
		}

		void UpdateButton(string uiName, string value)
		{
			if (uiName == "Button.Width")
			{
				if (currentButton==null)
				{
					return ;
				}
				var rect = currentButton.GetComponent<RectTransform>();
				rect.sizeDelta = new Vector2(float.Parse(value), rect.sizeDelta.y);

				currentLayout.UpdateLayout();
				return ;
			}

			if (uiName == "Button.Height")
			{
				if (currentButton==null)
				{
					return ;
				}
				var rect = currentButton.GetComponent<RectTransform>();
				rect.sizeDelta = new Vector2(rect.sizeDelta.x, float.Parse(value));

				currentLayout.UpdateLayout();
				return ;
			}

			if (uiName == "Panel.Width")
			{
				var rect = currentLayout.GetComponent<RectTransform>();

				rect.sizeDelta = new Vector2(float.Parse(value), rect.sizeDelta.y);

				currentLayout.UpdateLayout();
				return ;
			}

			if (uiName == "Panel.Height")
			{
				var rect = currentLayout.GetComponent<RectTransform>();

				rect.sizeDelta = new Vector2(rect.sizeDelta.x, float.Parse(value));

				currentLayout.UpdateLayout();
				return ;
			}

			if (uiName == "ButtonText")
			{
				return ;
			}
		}

		void UpdateButton(string uiName, bool value)
		{
			if (currentButton == null)
			{
				return ;
			}

			if (uiName == "Active")
			{
				currentButton.SetActive(value);

				currentLayout.UpdateLayout();
			}
		}

		void SelectButton(GameObject button)
		{
			foreach (Transform buttonTransform in views["Playground"].transform.Find("Layout"))
			{
				buttonTransform.gameObject.GetComponent<Button>().interactable = true;
			}
			
			currentButton = button;
			currentButton.GetComponent<Button>().interactable = false;

			var rect = currentButton.GetComponent<RectTransform>().rect;

			ButtonParametres.transform.Find("Button.Width").GetComponent<InputField>().text = rect.width.ToString();
			ButtonParametres.transform.Find("Button.Height").GetComponent<InputField>().text = rect.height.ToString();
		}

		string Enum2String<TEnum>(TEnum value)
#if CSHARP_7_3_OR_NEWER
		where TEnum : struct, Enum
#else
		where TEnum : struct
#endif
		{
			return EnumHelper<TEnum>.Instance.ToString(value);
		}

		public void Exit()
		{
			Application.Quit();
		}
	}
}