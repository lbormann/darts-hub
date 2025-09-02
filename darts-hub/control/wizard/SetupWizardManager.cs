using Avalonia.Controls;
using darts_hub.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Manages the setup wizard flow and coordinates wizard steps
    /// </summary>
    public class SetupWizardManager
    {
        private List<IWizardStep> wizardSteps;
        private int currentStepIndex;
        private WizardWindow wizardWindow;
        private Profile selectedProfile;
        private ProfileManager profileManager;
        private Configurator configurator;
        private ExtensionSelectionWizardStep extensionSelectionStep;

        public SetupWizardManager(ProfileManager profileManager, Configurator configurator)
        {
            this.profileManager = profileManager;
            this.configurator = configurator;
            this.wizardSteps = new List<IWizardStep>();
            this.currentStepIndex = 0;
        }

        /// <summary>
        /// Initializes the wizard with available steps
        /// </summary>
        public void InitializeWizardSteps(Profile profile)
        {
            selectedProfile = profile;
            wizardSteps.Clear();
            currentStepIndex = 0;

            // Add wizard steps in order
            wizardSteps.Add(new WelcomeWizardStep());
            
            // Add extension selection step
            extensionSelectionStep = new ExtensionSelectionWizardStep();
            wizardSteps.Add(extensionSelectionStep);
            
            // Caller setup is always included (mandatory)
            wizardSteps.Add(new CallerSetupWizardStep());
            
            // Extension-specific steps will be added dynamically in CreateDynamicSteps()
            
            // Add completion step
            wizardSteps.Add(new CompletionWizardStep());

            // Initialize all static steps
            foreach (var step in wizardSteps)
            {
                step.Initialize(profile, profileManager, configurator);
            }
        }

        /// <summary>
        /// Creates dynamic wizard steps based on user selection
        /// </summary>
        private void CreateDynamicSteps()
        {
            if (extensionSelectionStep?.SelectedExtensions == null)
                return;

            var selectedExtensions = extensionSelectionStep.SelectedExtensions;
            var dynamicSteps = new List<IWizardStep>();

            // Add WLED step if selected and available
            if (selectedExtensions.Contains("wled") || selectedExtensions.Any(e => e.Contains("wled")))
            {
                var wledApp = selectedProfile.Apps.Values.FirstOrDefault(a => 
                    a.App.CustomName.ToLower().Contains("wled"));
                if (wledApp != null)
                {
                    var wledStep = new WledSetupWizardStep();
                    wledStep.Initialize(selectedProfile, profileManager, configurator);
                    dynamicSteps.Add(wledStep);
                }
            }

            // Add Pixelit step if selected and available
            if (selectedExtensions.Contains("pixelit") || selectedExtensions.Any(e => e.Contains("pixelit")))
            {
                var pixelitApp = selectedProfile.Apps.Values.FirstOrDefault(a => 
                    a.App.CustomName.ToLower().Contains("pixelit"));
                if (pixelitApp != null)
                {
                    var pixelitStep = new PixelitSetupWizardStep();
                    pixelitStep.Initialize(selectedProfile, profileManager, configurator);
                    dynamicSteps.Add(pixelitStep);
                }
            }

            // Add Voice step if selected and available
            if (selectedExtensions.Contains("voice") || selectedExtensions.Any(e => e.Contains("voice")))
            {
                var voiceApp = selectedProfile.Apps.Values.FirstOrDefault(a => 
                    a.App.CustomName.ToLower().Contains("voice"));
                if (voiceApp != null)
                {
                    var voiceStep = new VoiceConfigWizardStep();
                    voiceStep.Initialize(selectedProfile, profileManager, configurator);
                    dynamicSteps.Add(voiceStep);
                }
            }

            // Add GIF step if selected and available
            if (selectedExtensions.Contains("gif") || selectedExtensions.Any(e => e.Contains("gif")))
            {
                var gifApp = selectedProfile.Apps.Values.FirstOrDefault(a => 
                    a.App.CustomName.ToLower().Contains("gif"));
                if (gifApp != null)
                {
                    var gifStep = new GifConfigWizardStep();
                    gifStep.Initialize(selectedProfile, profileManager, configurator);
                    dynamicSteps.Add(gifStep);
                }
            }

            // Add Extern step if selected and available
            if (selectedExtensions.Contains("extern") || selectedExtensions.Any(e => e.Contains("extern")))
            {
                var externApp = selectedProfile.Apps.Values.FirstOrDefault(a => 
                    a.App.CustomName.ToLower().Contains("extern"));
                if (externApp != null)
                {
                    var externStep = new ExternConfigWizardStep();
                    externStep.Initialize(selectedProfile, profileManager, configurator);
                    dynamicSteps.Add(externStep);
                }
            }

            // Insert dynamic steps before the completion step
            var completionStepIndex = wizardSteps.Count - 1;
            for (int i = 0; i < dynamicSteps.Count; i++)
            {
                wizardSteps.Insert(completionStepIndex + i, dynamicSteps[i]);
            }
        }

        /// <summary>
        /// Shows the wizard window
        /// </summary>
        public async Task<bool> ShowWizard(Window parentWindow)
        {
            wizardWindow = new WizardWindow(this);
            
            // Show the first step
            if (wizardSteps.Count > 0)
            {
                await ShowCurrentStep();
            }

            var result = await wizardWindow.ShowDialog<bool>(parentWindow);
            return result;
        }

        /// <summary>
        /// Gets the current wizard step
        /// </summary>
        public IWizardStep GetCurrentStep()
        {
            if (currentStepIndex >= 0 && currentStepIndex < wizardSteps.Count)
            {
                return wizardSteps[currentStepIndex];
            }
            return null;
        }

        /// <summary>
        /// Navigates to the next wizard step
        /// </summary>
        public async Task<bool> GoToNextStep()
        {
            var currentStep = GetCurrentStep();
            if (currentStep == null) return false;

            // Validate current step before proceeding
            var validationResult = await currentStep.ValidateStep();
            if (!validationResult.IsValid)
            {
                // Show validation error
                await ShowValidationError(validationResult.ErrorMessage);
                return false;
            }

            // Apply the step configuration
            await currentStep.ApplyConfiguration();

            // Special handling after extension selection step
            if (currentStep is ExtensionSelectionWizardStep)
            {
                CreateDynamicSteps();
            }

            // Move to next step
            if (currentStepIndex < wizardSteps.Count - 1)
            {
                currentStepIndex++;
                await ShowCurrentStep();
                return true;
            }
            else
            {
                // Wizard completed
                await CompleteWizard();
                return true;
            }
        }

        /// <summary>
        /// Navigates to the previous wizard step
        /// </summary>
        public async Task GoToPreviousStep()
        {
            if (currentStepIndex > 0)
            {
                // Special handling: if going back from a dynamic step, we might need to rebuild
                var currentStep = GetCurrentStep();
                bool isDynamicStep = !(currentStep is WelcomeWizardStep || 
                                     currentStep is ExtensionSelectionWizardStep || 
                                     currentStep is CallerSetupWizardStep || 
                                     currentStep is CompletionWizardStep);

                currentStepIndex--;
                
                // If we went back to extension selection, remove dynamic steps
                if (GetCurrentStep() is ExtensionSelectionWizardStep && isDynamicStep)
                {
                    RemoveDynamicSteps();
                }
                
                await ShowCurrentStep();
            }
        }

        /// <summary>
        /// Removes dynamically created steps
        /// </summary>
        private void RemoveDynamicSteps()
        {
            var staticSteps = wizardSteps.Where(step => 
                step is WelcomeWizardStep || 
                step is ExtensionSelectionWizardStep || 
                step is CallerSetupWizardStep || 
                step is CompletionWizardStep).ToList();

            wizardSteps.Clear();
            wizardSteps.AddRange(staticSteps);
        }

        /// <summary>
        /// Shows the current wizard step
        /// </summary>
        private async Task ShowCurrentStep()
        {
            var currentStep = GetCurrentStep();
            if (currentStep != null && wizardWindow != null)
            {
                await wizardWindow.DisplayStep(currentStep);
            }
        }

        /// <summary>
        /// Displays validation error to user
        /// </summary>
        private async Task ShowValidationError(string errorMessage)
        {
            // This will be handled by the WizardWindow
            if (wizardWindow != null)
            {
                await wizardWindow.ShowValidationError(errorMessage);
            }
        }

        /// <summary>
        /// Completes the wizard and saves configuration
        /// </summary>
        private async Task CompleteWizard()
        {
            try
            {
                // Save all configurations
                profileManager.StoreApps();
                
                // Mark wizard as completed
                configurator.Settings.WizardCompleted = true;
                configurator.SaveSettings();

                // Close wizard window
                if (wizardWindow != null)
                {
                    wizardWindow.Close(true);
                }
            }
            catch (Exception ex)
            {
                await ShowValidationError($"Failed to save configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancels the wizard
        /// </summary>
        public void CancelWizard()
        {
            if (wizardWindow != null)
            {
                wizardWindow.Close(false);
            }
        }

        /// <summary>
        /// Gets the progress of the wizard (0.0 to 1.0)
        /// </summary>
        public double GetProgress()
        {
            if (wizardSteps.Count == 0) return 0.0;
            return (double)(currentStepIndex + 1) / wizardSteps.Count;
        }

        /// <summary>
        /// Gets the current step number (1-based)
        /// </summary>
        public int GetCurrentStepNumber()
        {
            return currentStepIndex + 1;
        }

        /// <summary>
        /// Gets the total number of steps
        /// </summary>
        public int GetTotalSteps()
        {
            return wizardSteps.Count;
        }

        /// <summary>
        /// Checks if the wizard can go to the next step
        /// </summary>
        public bool CanGoNext()
        {
            return currentStepIndex < wizardSteps.Count - 1;
        }

        /// <summary>
        /// Checks if the wizard can go to the previous step
        /// </summary>
        public bool CanGoPrevious()
        {
            return currentStepIndex > 0;
        }

        /// <summary>
        /// Checks if the current step is the last step
        /// </summary>
        public bool IsLastStep()
        {
            return currentStepIndex == wizardSteps.Count - 1;
        }
    }
}