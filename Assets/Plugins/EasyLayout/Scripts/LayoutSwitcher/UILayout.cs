﻿namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// User interface layout.
	/// </summary>
	[Serializable]
	public class UILayout
	{
		/// <summary>
		/// The name.
		/// </summary>
		[SerializeField]
		public string Name;

		/// <summary>
		/// The aspect ratio.
		/// </summary>
		[SerializeField]
		public Vector2 AspectRatio;

		/// <summary>
		/// The aspect ratio as float.
		/// </summary>
		public float AspectRatioFloat
		{
			get
			{
				return AspectRatio.x / AspectRatio.y;
			}
		}

		/// <summary>
		/// The maximum display size.
		/// </summary>
		[SerializeField]
		public float MaxDisplaySize;

		/// <summary>
		/// The positions.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		public List<UIPosition> Positions = new List<UIPosition>();

		/// <summary>
		/// Save the specified objects positions.
		/// </summary>
		/// <param name="objects">Objects.</param>
		public void Save(List<RectTransform> objects)
		{
			Convert(objects, Positions);
		}

		/// <summary>
		/// Convert list.
		/// </summary>
		/// <param name="objects">Objects to convert.</param>
		/// <param name="output">Positions.</param>
		protected static void Convert(List<RectTransform> objects, List<UIPosition> output)
		{
			output.Clear();

			for (int i = 0; i < objects.Count; i++)
			{
				output.Add(SavePosition(objects[i]));
			}
		}

		/// <summary>
		/// Save the object position.
		/// </summary>
		/// <returns>The position.</returns>
		/// <param name="obj">Object.</param>
		protected static UIPosition SavePosition(RectTransform obj)
		{
			if (obj == null)
			{
				return null;
			}

			var position = new UIPosition
			{
				Object = obj,
				Active = obj.gameObject.activeSelf,

				Position = obj.localPosition,

				AnchorMax = obj.anchorMax,
				AnchorMin = obj.anchorMin,
				SizeDelta = obj.sizeDelta,

				Pivot = obj.pivot,

				Rotation = obj.localRotation.eulerAngles,
				Scale = obj.localScale,
			};

			return position;
		}

		/// <summary>
		/// Load this instance.
		/// </summary>
		public void Load()
		{
			foreach (var p in Positions)
			{
				Load(p);
			}
		}

		/// <summary>
		/// Load the specified position.
		/// </summary>
		/// <param name="position">Position.</param>
		static void Load(UIPosition position)
		{
			if (position == null)
			{
				return;
			}

			if (position.Object == null)
			{
				return;
			}

			var obj = position.Object;
			obj.gameObject.SetActive(position.Active);

			obj.localPosition = position.Position;

			obj.anchorMax = position.AnchorMax;
			obj.anchorMin = position.AnchorMin;
			obj.sizeDelta = position.SizeDelta;

			obj.pivot = position.Pivot;

			obj.localRotation = Quaternion.Euler(position.Rotation);

			obj.localScale = position.Scale;

			obj.localPosition = position.Position;
		}
	}
}