using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TwitchIntegration;
using TwitchIntegration.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TwitchIntegrationEditor
{
    public class TwitchCommandsEditor : EditorWindow
    {
        private enum Tab
        {
            Commands,
            Settings
        }
        
        private VisualElement _commandsTabRightPane;
        private ListView _commandsTabLeftPane;

        private VisualElement _redirectUriError;
        private VisualElement _portWarning;
        
        private static TwitchSettings _settings;
        
        private static Action _onClientIDFilled;
        
        private static bool IsParameterSupported(Type type) => type == typeof(TwitchUser) ||
            type == typeof(string) || type == typeof(int) || type == typeof(float) || type == typeof(bool);
        
        private static bool InheritsFromTwitchMonoBehaviour(Type type) => 
            type.IsSubclassOf(typeof(TwitchMonoBehaviour));

        private const string REGISTER_APP_URL = "https://dev.twitch.tv/docs/authentication/register-app";

        private const string DEV_CREDITS = "Made by <color=#7ED4FB><u>Danial Jumagaliyev</u></color>";
        private const string DEV_URL = "https://www.danqzq.games";

        [MenuItem("Dan's Tools/Twitch Commands")]
        public static void ShowEditorWindow()
        {
            EditorWindow wnd = GetWindow<TwitchCommandsEditor>();
            wnd.titleContent = new GUIContent("Twitch");
        }

        private static void InitializeSettings()
        {
            _settings = Resources.Load<TwitchSettings>("TwitchSettings");
        }

        private void CreateGUI()
        {
            InitializeSettings();
            rootVisualElement.Clear();
            var commandsTab = PopulateCommandsTab();
            var settingsTab = PopulateSettingsTab();

            var toolbar = new Toolbar();
            var dropdown = new EnumField(Tab.Commands);
            toolbar.RegisterCallback(new EventCallback<ChangeEvent<Enum>>(_ =>
            {
                rootVisualElement.Clear();
                rootVisualElement.Add(toolbar);
                rootVisualElement.Add((Tab) dropdown.value == Tab.Commands ? commandsTab : settingsTab);
            }));
            toolbar.Add(dropdown);
            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(commandsTab);
        }
        
        #region Commands Tab

        private VisualElement PopulateCommandsTab()
        {
            var commandsTab = new TwoPaneSplitView(0, 275, TwoPaneSplitViewOrientation.Horizontal);
            
            if (_settings.commandList.Count == 0)
            {
                var helpBox = new HelpBox("No commands have been created yet. Create a method with the " +
                                          "'TwitchCommand' attribute in your scripts to make a command.", HelpBoxMessageType.Info); 
                commandsTab.Add(helpBox);
                commandsTab.Add(new VisualElement());
                return commandsTab;
            }

            // A TwoPaneSplitView always needs exactly two child elements
            _commandsTabLeftPane = new ListView();
            commandsTab.Add(_commandsTabLeftPane);
            _commandsTabRightPane = new VisualElement();
            commandsTab.Add(_commandsTabRightPane);

            var methodCollection = TypeCache.GetMethodsWithAttribute(typeof(TwitchCommandAttribute));

            if (string.IsNullOrEmpty(_settings.clientId))
            {
                var helpBox = new HelpBox("Please set your game's Twitch Client ID in the Settings tab",
                    HelpBoxMessageType.Warning);
                var linkToDocumentation = new Button(() => Application.OpenURL(REGISTER_APP_URL))
                {
                    text = "How do I get my Twitch Client ID?"
                };
                _commandsTabRightPane.Add(helpBox);
                _commandsTabRightPane.Add(linkToDocumentation);

                _onClientIDFilled = () =>
                {
                    if (_commandsTabRightPane.Contains(helpBox)) 
                        _commandsTabRightPane.Remove(helpBox);
                    if (_commandsTabRightPane.Contains(linkToDocumentation)) 
                        _commandsTabRightPane.Remove(linkToDocumentation);
                };
            }
            
            if (HasDuplicateMethodNames(methodCollection))
                return commandsTab;

            _commandsTabLeftPane.makeItem = () =>
            {
                var panes = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
                panes.Add(new Label
                {
                    style = {paddingLeft = 5, unityTextAlign = TextAnchor.MiddleLeft}
                });
                panes.Add(new Toggle());
                return panes;
            };
            _commandsTabLeftPane.bindItem = (item, index) =>
            {
                var children = item.Children().ToArray();
                var label = (Label) children[0];
                var toggle = (Toggle) children[1];
                label.text = _settings.commandList[index].name;
                if (!InheritsFromTwitchMonoBehaviour(methodCollection[index].DeclaringType) || 
                    methodCollection[index].GetParameters().Any(p => !IsParameterSupported(p.ParameterType)))
                    label.style.color = Color.red;
                toggle.SetValueWithoutNotify(_settings.commandList[index].enabled);
                toggle.RegisterCallback(new EventCallback<ChangeEvent<bool>>(_ =>
                    _settings.commandList[index].enabled = toggle.value));
            };
            _commandsTabLeftPane.itemsSource = methodCollection;
            _commandsTabLeftPane.onSelectionChange += OnMethodSelected;
            
            _commandsTabLeftPane.hierarchy.Add(CreateCreditLabel(Align.Auto));

            return commandsTab;
        }

        private bool HasDuplicateMethodNames(IEnumerable<MethodInfo> methodCollection)
        {
            var duplicateNames = methodCollection.GroupBy(m => 
                    m.GetCustomAttribute<TwitchCommandAttribute>().Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            var enumerable = duplicateNames as string[] ?? duplicateNames.ToArray();
            
            if (!enumerable.Any()) return false;
            var helpBox = new HelpBox
            {
                text = "There are commands with duplicate names! Please make sure to use unique names for each command to avoid conflicts.\n" +
                       "Duplicate commands: " + string.Join(", ", enumerable),
                messageType = HelpBoxMessageType.Error
            };
            _commandsTabRightPane.Add(helpBox);
            
            return true;
        }

        private void OnMethodSelected(IEnumerable<object> obj)
        {
            _commandsTabRightPane.Clear();
            var method = (MethodInfo) obj.First();

            _commandsTabRightPane.Add(new TextElement
            {
                text = method.Name.ToBold(),
                style = {fontSize = 24, paddingLeft = 5}
            });

            if (!InheritsFromTwitchMonoBehaviour(method.DeclaringType))
                _commandsTabRightPane.Add(new HelpBox("The declaring class of the command " +
                                            "is not inherited from TwitchMonoBehaviour!\n" +
                                            "It will not execute in runtime!", HelpBoxMessageType.Error));
            
            var infoLabel = new Label("Info:".ToBold())
            {
                style = {paddingLeft = 5}
            };
            _commandsTabRightPane.Add(infoLabel);

            var declaringClassField = new TextField
            {
                label = "Declaring Class",
                value = method.DeclaringType!.Name,
                tooltip = "The class that declares the command",
                style = {color = InheritsFromTwitchMonoBehaviour(method.DeclaringType) ? Color.white : Color.red}
            };
            _commandsTabRightPane.Add(declaringClassField);
            declaringClassField.SetEnabled(false);

            var attributeInfo = method.GetCustomAttribute<TwitchCommandAttribute>();
            var commandInfo = _settings.commandList.First(c => c.name == attributeInfo.Name);
            
            var aliasesField = new TextField
            {
                label = "Aliases",
                value = attributeInfo.Aliases.Any() ? string.Join(", ", attributeInfo.Aliases) : "None",
                tooltip = "Alternative names that the command can be called by"
            };
            _commandsTabRightPane.Add(aliasesField);
            aliasesField.SetEnabled(false);

            var cooldownTextField = new TextField
            {
                label = "Cooldown In Seconds",
                value = commandInfo.cooldown.ToString(CultureInfo.InvariantCulture)
            };
            cooldownTextField.RegisterCallback(new EventCallback<ChangeEvent<string>>(_ =>
            {
                var match = Regex.Match(cooldownTextField.value, 
                    @"[+-]?([0-9]*[.])?[0-9]+", RegexOptions.Singleline);
                if (match.Success)
                {
                    cooldownTextField.SetValueWithoutNotify(match.Value);
                    commandInfo.cooldown = float.Parse(match.Value);
                }
                else
                {
                    cooldownTextField.SetValueWithoutNotify(commandInfo.cooldown.ToString(CultureInfo.InvariantCulture));
                }
            }));
            _commandsTabRightPane.Add(cooldownTextField);

            var parameters = method.GetParameters().ToList();
            if (parameters.Count == 0) return;
            
            _commandsTabRightPane.Add(new Label("")); //just a little space
        
            var parametersLabel = new Label("Command Arguments:".ToBold())
            {
                style = {paddingLeft = 5}
            };
            _commandsTabRightPane.Add(parametersLabel);

            parameters.ForEach(parameter =>
            {
                var parameterLabel = new Label($"{parameter.Name} : {DataTypeToString(parameter.ParameterType)}")
                {
                    style = {paddingLeft = 5}
                };
                if (!IsParameterSupported(parameter.ParameterType))
                {
                    parameterLabel.text += " [Unsupported] ";
                    parameterLabel.style.color = Color.red;
                }
                _commandsTabRightPane.Add(parameterLabel);
            });
        }
        
        #endregion
        
        private VisualElement PopulateSettingsTab()
        {
            var settingsTab = new VisualElement();
            settingsTab.Add(new TextElement
            {
                text = "Settings".ToBold(),
                style = {fontSize = 24}
            });
            
            _redirectUriError = new HelpBox("The redirect URI must end with a forward slash ('/')! " +
                                            "Make sure it is written exactly as is in the list of OAuth Redirect URLs " +
                                            "in your Twitch Application settings.", HelpBoxMessageType.Error);

            _portWarning = new HelpBox("In case of errors in authentication, try changing the redirect URI to include a " +
                                       "port (a set of 4-5 numbers separated by a colon from the end of the url, " + 
                                       "e.g http://localhost:8080/). Make sure the port is also included in " + 
                                       "the Redirect URL in your Twitch Application settings.",
                                       HelpBoxMessageType.Warning);
            
            var clientIdField = new TextField
            {
                label = "Game Twitch Client ID",
                value = _settings.clientId,
                labelElement = {style = {color = string.IsNullOrEmpty(_settings.clientId) ? Color.yellow : Color.white}}
            };
            clientIdField.RegisterCallback(new EventCallback<ChangeEvent<string>>(_ =>
            {
                _settings.clientId = clientIdField.value;
                clientIdField.labelElement.style.color = string.IsNullOrEmpty(clientIdField.value) ? 
                    Color.yellow : Color.white;
                EditorUtility.SetDirty(_settings);
                _onClientIDFilled?.Invoke();
            }));
            settingsTab.Add(clientIdField);
            
            var commandPrefixField = new TextField
            {
                label = "Command Prefix",
                value = _settings.commandPrefix
            };
            commandPrefixField.RegisterCallback(new EventCallback<ChangeEvent<string>>(_ =>
            {
                _settings.commandPrefix = commandPrefixField.value;
                EditorUtility.SetDirty(_settings);
            }));
            settingsTab.Add(commandPrefixField);
            
            var redirectUriField = new TextField
            {
                label = "Redirect URI",
                value = _settings.redirectUri
            };
            redirectUriField.RegisterCallback(new EventCallback<ChangeEvent<string>>(_ =>
            {
                _settings.redirectUri = redirectUriField.value;
                EditorUtility.SetDirty(_settings);
                
                if (!settingsTab.Contains(_redirectUriError))
                    settingsTab.Add(_redirectUriError);
                if (redirectUriField.value.EndsWith("/"))
                    settingsTab.Remove(_redirectUriError);

                if (!settingsTab.Contains(_portWarning))
                    settingsTab.Add(_portWarning);
                if (redirectUriField.value.Replace("://", "").Contains(":"))
                    settingsTab.Remove(_portWarning);
            }));
            settingsTab.Add(redirectUriField);
            
            if (!redirectUriField.value.EndsWith("/"))
                settingsTab.Add(_redirectUriError);
            
            if (!redirectUriField.value.Replace("://", "").Contains(":"))
                settingsTab.Add(_portWarning);

            var commandsMode = new EnumField("Commands Mode", _settings.commandsMode);
            commandsMode.RegisterCallback(new EventCallback<ChangeEvent<Enum>>(_ =>
            {
                _settings.commandsMode = (TwitchCommandsMode) commandsMode.value;
                EditorUtility.SetDirty(_settings);
            }));
            settingsTab.Add(commandsMode);
            
            var initializeOnAwakeField = new Toggle
            {
                label = "Initialize On Awake",
                value = _settings.initializeOnAwake,
                tooltip = "If enabled, Twitch will initialize on Awake. If disabled, you must call Initialize() manually."
            };
            initializeOnAwakeField.RegisterCallback(new EventCallback<ChangeEvent<bool>>(_ =>
            {
                _settings.initializeOnAwake = initializeOnAwakeField.value;
                EditorUtility.SetDirty(_settings);
            }));
            settingsTab.Add(initializeOnAwakeField);
            
            var isDebugModeField = new Toggle
            {
                label = "Is Debug Mode",
                value = _settings.isDebugMode,
                tooltip = "If enabled, you will receive debug messages in the console."
            };
            isDebugModeField.RegisterCallback(new EventCallback<ChangeEvent<bool>>(_ =>
            {
                _settings.isDebugMode = isDebugModeField.value;
                EditorUtility.SetDirty(_settings);
            }));
            settingsTab.Add(isDebugModeField);
            
            settingsTab.Add(new Label
            {
                text = "v" + TwitchVariables.Version,
                style = {alignSelf = Align.FlexEnd}
            });
            
            settingsTab.Add(CreateCreditLabel(Align.FlexEnd));
            
            return settingsTab;
        }

        private static Label CreateCreditLabel(Align alignment)
        {
            var creditLabel = new Label(DEV_CREDITS);
            creditLabel.RegisterCallback(new EventCallback<PointerDownEvent> (_ =>
                Application.OpenURL(DEV_URL)));
            creditLabel.style.alignSelf = alignment;
            creditLabel.SendToBack();
            return creditLabel;
        }

        private static string DataTypeToString(Type type)
        {
            return type == typeof(string) ? "string" :
                type == typeof(int) ? "int" :
                type == typeof(float) ? "float" :
                type == typeof(bool) ? "bool" : type.Name;
        }
    }
}
