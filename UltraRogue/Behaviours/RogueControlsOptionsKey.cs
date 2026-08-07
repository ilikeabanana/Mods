using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ultrarogue
{
    // Reuses the game's ControlsOptionsKey prefab/GameObject, but drives it
    // with our own InputAction/InputControlScheme instead of the base game's,
    // and doesn't depend on MonoSingleton<InputManager>.Instance.defaultActions
    // (which has no entry for our custom action -> was the source of the NRE).
    public class RogueControlsOptionsKey : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler
    {
        public TextMeshProUGUI actionText;
        public Button restoreDefaultsButton;
        public GameObject bindingButtonTemplate;
        public Transform bindingButtonParent;
        public Selectable selectable;
        public GameObject blocker;

        private List<Button> bindingButtons = new List<Button>();
        private bool selected;
        private readonly Color faintTextColor = new Color(1f, 1f, 1f, 0.15f);

        // Call this once after AddComponent, passing the existing ControlsOptionsKey
        // found on the same GameObject, to pull field references and disable it.
        public void Init(ControlsOptionsKey source)
        {
            actionText = source.actionText;
            restoreDefaultsButton = source.restoreDefaultsButton;
            bindingButtonTemplate = source.bindingButtonTemplate;
            bindingButtonParent = source.bindingButtonParent;
            selectable = source.selectable;
            blocker = source.blocker;

            source.enabled = false;
        }

        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;

        private void SubmitPressed(InputAction.CallbackContext ctx)
        {
            if (selected && bindingButtons.Count > 0)
                bindingButtons[0].Select();
        }

        private void OnEnable()
        {
            MonoSingleton<InputManager>.Instance.InputSource.Actions.UI.Submit.performed += SubmitPressed;
        }

        private void OnDisable()
        {
            if (MonoSingleton<InputManager>.Instance)
                MonoSingleton<InputManager>.Instance.InputSource.Actions.UI.Submit.performed -= SubmitPressed;
        }

        private void Update()
        {
            bool active = MonoSingleton<InputManager>.Instance.LastButtonDevice is Gamepad;
            blocker.SetActive(active);
        }

        public void RebuildBindings(InputAction action, InputControlScheme controlScheme)
        {
            foreach (Button button in bindingButtons)
                Destroy(button.gameObject);
            bindingButtons.Clear();

            int count = 0;
            int[] bindingsWithGroup = action.GetBindingsWithGroup(controlScheme.bindingGroup);

            for (int i = 0; i < bindingsWithGroup.Length; i++)
            {
                int index2 = bindingsWithGroup[i];
                InputBinding binding = action.bindings[index2];
                count++;

                string bindingDisplayString = action.GetBindingDisplayString(index2, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                var (btn, txt, img, tooltip) = BuildBindingButton(bindingDisplayString);

                string tooltipText = txt.text + "<br>";
                bool hasConflict = false;

                if (binding.isComposite)
                {
                    var bindingSyntax = action.ChangeBinding(binding).NextBinding();
                    HashSet<string> seen = new HashSet<string>();
                    while (bindingSyntax.valid)
                    {
                        if (!bindingSyntax.binding.isPartOfComposite)
                            break;

                        InputBinding[] conflicts = MonoSingleton<InputManager>.Instance.InputSource.GetConflicts(bindingSyntax.binding);
                        if (conflicts.Length != 0 && !seen.Contains(bindingSyntax.binding.path))
                        {
                            hasConflict = true;
                            tooltipText += "<br>" + GenerateTooltip(action, bindingSyntax.binding, conflicts);
                            seen.Add(bindingSyntax.binding.path);
                        }
                        bindingSyntax = bindingSyntax.NextBinding();
                    }
                }
                else
                {
                    InputBinding[] conflicts = MonoSingleton<InputManager>.Instance.InputSource.GetConflicts(binding);
                    if (conflicts.Length != 0)
                    {
                        hasConflict = true;
                        tooltipText += "<br>" + GenerateTooltip(action, binding, conflicts);
                    }
                }

                tooltip.text = tooltipText;
                tooltip.enabled = true;
                if (hasConflict)
                    txt.color = Color.red;

                int index = index2;
                btn.onClick.AddListener(() =>
                {
                    Color c = img.color;
                    img.color = Color.red;

                    if (binding.isComposite)
                    {
                        MonoSingleton<InputManager>.Instance.RebindComposite(
                            action,
                            index,
                            part => txt.text = "PRESS " + part.ToUpper(),
                            () => RebuildBindings(action, controlScheme),
                            () =>
                            {
                                action.ChangeBinding(index).Erase();
                                MonoSingleton<InputManager>.Instance.actionModified?.Invoke(action);
                            },
                            controlScheme);
                        return;
                    }

                    MonoSingleton<InputManager>.Instance.Rebind(
                        action,
                        index,
                        () =>
                        {
                            RebuildBindings(action, controlScheme);
                        },
                        () =>
                        {
                            action.ChangeBinding(index).Erase();
                            MonoSingleton<InputManager>.Instance.actionModified?.Invoke(action);
                        },
                        controlScheme);
                });
            }

            if (count < 4)
            {
                var (btn, txt, img) = BuildNewBindButton();
                btn.onClick.AddListener(() =>
                {
                    img.color = Color.red;
                    txt.color = Color.white;
                    txt.text = "...";

                    if (action.expectedControlType == "Button")
                    {
                        MonoSingleton<InputManager>.Instance.Rebind(
                            action,
                            null,
                            () =>
                            {
                                RebuildBindings(action, controlScheme);
                            },
                            () => RebuildBindings(action, controlScheme),
                            controlScheme);
                    }
                    else if (action.expectedControlType == "Vector2")
                    {
                        MonoSingleton<InputManager>.Instance.RebindComposite(
                            action,
                            null,
                            part => txt.text = "PRESS " + part.ToUpper(),
                            () => RebuildBindings(action, controlScheme),
                            () => RebuildBindings(action, controlScheme),
                            controlScheme);
                    }
                });
            }

            // "At default" = current path matches the captured design-time default.
            bool hasOverride = bindingsWithGroup.Any(idx =>
            {
                string defaultPath = RogueInputSave.GetDefaultPath(action, idx);
                return defaultPath != null && action.bindings[idx].path != defaultPath;
            });
            restoreDefaultsButton.gameObject.SetActive(hasOverride);
            restoreDefaultsButton.onClick.RemoveAllListeners();
            restoreDefaultsButton.onClick.AddListener(() =>
            {
                foreach (int idx in bindingsWithGroup)
                {
                    string defaultPath = RogueInputSave.GetDefaultPath(action, idx);
                    if (defaultPath != null)
                        action.ChangeBinding(idx).WithPath(defaultPath);
                }
                RogueInputSave.SaveBindings(action, controlScheme);
                RebuildBindings(action, controlScheme);
            });

            RogueInputSave.SaveBindings(action, controlScheme);

            Navigation nav = selectable.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnRight = bindingButtons[0];
            selectable.navigation = nav;
        }

        private (Button, TextMeshProUGUI, Image) BuildNewBindButton()
        {
            var (btn, txt, img, _) = BuildBindingButton("+");
            txt.color = faintTextColor;
            txt.fontSizeMax = 27f;
            return (btn, txt, img);
        }

        private string GenerateTooltip(InputAction action, InputBinding binding, InputBinding[] conflicts)
        {
            string str = action.GetBindingDisplayStringWithoutOverride(
                binding, InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpper();
            string result = "<color=red>" + str + " IS BOUND MULTIPLE TIMES:";
            HashSet<string> seen = new HashSet<string>();
            foreach (var b in conflicts)
            {
                if (!seen.Contains(b.action))
                {
                    result += "<br>- " + b.action.ToUpper();
                    seen.Add(b.action);
                }
            }
            return result + "</color>";
        }

        private (Button, TextMeshProUGUI, Image, TooltipOnHover) BuildBindingButton(string text)
        {
            GameObject go = Instantiate(bindingButtonTemplate, bindingButtonParent);
            TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            Button button = go.GetComponent<Button>();
            Image image = go.GetComponent<Image>();
            TooltipOnHover tooltip = go.GetComponent<TooltipOnHover>();
            tmp.text = text;
            bindingButtons.Add(button);
            go.SetActive(true);
            return (button, tmp, image, tooltip);
        }
    }
}