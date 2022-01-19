using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

namespace ExpressionUtility.UI
{
	internal class AvatarSelection : IExpressionUI
	{
		private Dictionary<AvatarCache.AvatarInfo, VisualElement> _buttons = new Dictionary<AvatarCache.AvatarInfo, VisualElement>();
		private UIController _controller;
		private Messages _messages;

		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			_controller = controller;
			_messages = controller.Messages;
			if (controller.AvatarCache.AvatarCount == 1 && controller.ExpressionInfo.AvatarDescriptor != null && previousUI is Intro)
			{
				controller.SetFrame<Setup>();
				return;
			}

			_buttons.Clear();
			SetupButtons();
			_controller.AvatarCache.AvatarWasUpdated += OnAvatarWasUpdated;
		}

		private void UpdateInfo()
		{
			_messages.SetActive(_buttons.Any(),"select-avatar");
			_messages.SetActive(!_buttons.Any(), "scene-empty");
		}
		
		private void OnAvatarWasUpdated(AvatarCache.AvatarInfo info)
		{
			if (!info.IsValid)
			{
				if(_buttons.TryGetValue(info, out var button))
				{
					var frame = _controller.ContentFrame.Q("avatars");
					frame.Remove(button);
				}

				_buttons.Remove(info);
				UpdateInfo();
				return;
			}
			CreateOrUpdateButton(info);
			UpdateInfo();
		}

		private void SetupButtons()
		{
			foreach (AvatarCache.AvatarInfo avatarInfo in _controller.AvatarCache.GetAllAvatarInfo())
			{
				CreateOrUpdateButton(avatarInfo);
			}
			UpdateInfo();
		}


		public void OnExit(IExpressionUI nextUI)
		{
			_controller.AvatarCache.AvatarWasUpdated -= OnAvatarWasUpdated;
			if (nextUI is Setup)
			{
				_controller.ExpressionInfo.Ping();
			}
		}


		private void CreateOrUpdateButton(AvatarCache.AvatarInfo info)
		{
			if (!info.IsValid)
			{
				return;
			}
			
			if(!_buttons.TryGetValue(info, out var element))
			{
				var frame = _controller.ContentFrame.Q("avatars");
				element = _controller.AssetsReferences.AvatarSelectorButton.InstantiateTemplate(frame);
			}

			void Clicked() => AvatarClicked(info);

			_buttons[info] = element;
			element.style.display = DisplayStyle.Flex;

			element.Q<Button>().clickable = new Clickable(Clicked);
			element.Q("thumbnail").style.backgroundImage = info.Thumbnail;
			element.Q<Label>("header").text = info.Name;
			element.Q<Label>("description").text = info.Description;
			var ob = element.Q<ObjectField>("object-field");
			ob.objectType = typeof(GameObject);
			ob.allowSceneObjects = true;
			ob.SetEnabled(false);
			ob.value = info.VrcAvatarDescriptor.gameObject;
			ob.Q(null, "unity-object-field__selector").Display(false);
			ob.Q(null, "unity-label").Display(false);
			ob.Q(null, "unity-object-field-display__label").Display(true);
		}

		private void AvatarClicked(AvatarCache.AvatarInfo info)
		{
			_controller.ExpressionInfo.SetInfo(info);
			_controller.SetFrame<Setup>();
		}
	}
}