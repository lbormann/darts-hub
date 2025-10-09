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
        private bool isNavigating = false; // Flag to prevent concurrent navigation
        private HashSet<string> lastSelectedExtensions = new HashSet<string>(); // ? Track last selected extensions

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
            lastSelectedExtensions.Clear(); // ? Reset extension tracking

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
            
            // ? Check if extension selection has changed
            var currentExtensionsSet = new HashSet<string>(selectedExtensions);
            if (currentExtensionsSet.SetEquals(lastSelectedExtensions))
            {
                // No change in selection, don't rebuild steps
                System.Diagnostics.Debug.WriteLine("Extension selection unchanged, skipping dynamic step recreation");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Extension selection changed from [{string.Join(", ", lastSelectedExtensions)}] to [{string.Join(", ", currentExtensionsSet)}]");
            
            // ? Always remove existing dynamic steps before creating new ones
            RemoveDynamicSteps();
            
            // ? Update tracked extensions
            lastSelectedExtensions = currentExtensionsSet;

            var dynamicSteps = new List<IWizardStep>();

            // ? Add dynamic steps in the CORRECT order: AFTER Caller, BEFORE Completion
            // Order should be: Welcome -> ExtensionSelection -> Caller -> [Dynamic Extensions] -> Completion
            
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
                    System.Diagnostics.Debug.WriteLine("Added WLED setup step");
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
                    System.Diagnostics.Debug.WriteLine("Added Pixelit setup step");
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
                    System.Diagnostics.Debug.WriteLine("Added Voice setup step");
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
                    System.Diagnostics.Debug.WriteLine("Added GIF setup step");
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
                    System.Diagnostics.Debug.WriteLine("Added Extern setup step");
                }
            }

            // ? Insert dynamic steps at the correct position: after Caller (index 2), before Completion
            // Expected order: [0]Welcome -> [1]ExtensionSelection -> [2]Caller -> [3+]Dynamic -> [Last]Completion
            var callerStepIndex = wizardSteps.FindIndex(s => s is CallerSetupWizardStep);
            if (callerStepIndex == -1)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Could not find Caller step!");
                return;
            }

            // Insert dynamic steps after Caller step
            var insertIndex = callerStepIndex + 1;
            for (int i = 0; i < dynamicSteps.Count; i++)
            {
                wizardSteps.Insert(insertIndex + i, dynamicSteps[i]);
                System.Diagnostics.Debug.WriteLine($"Inserted {dynamicSteps[i].GetType().Name} at index {insertIndex + i}");
            }
            
            System.Diagnostics.Debug.WriteLine($"Total wizard steps after dynamic creation: {wizardSteps.Count}");
            
            // ? Debug: Print current step order
            for (int i = 0; i < wizardSteps.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine($"  [{i}] {wizardSteps[i].GetType().Name}");
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
            return await GoToNextStep(validateStep: true);
        }

        /// <summary>
        /// Skips the current wizard step without validation
        /// </summary>
        public async Task<bool> SkipCurrentStep()
        {
            return await GoToNextStep(validateStep: false);
        }

        /// <summary>
        /// Navigates to the next wizard step with optional validation
        /// </summary>
        private async Task<bool> GoToNextStep(bool validateStep)
        {
            // Prevent concurrent navigation
            if (isNavigating) return false;
            
            try
            {
                isNavigating = true;
                
                var currentStep = GetCurrentStep();
                if (currentStep == null) return false;

                System.Diagnostics.Debug.WriteLine($"Going to next step from: {currentStep.GetType().Name} (index {currentStepIndex})");

                if (validateStep)
                {
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
                }
                // When skipping, we don't validate or apply configuration

                // ? Special handling for extension selection step - ALWAYS check for changes
                if (currentStep is ExtensionSelectionWizardStep)
                {
                    System.Diagnostics.Debug.WriteLine("Processing extension selection changes...");
                    var oldStepCount = wizardSteps.Count;
                    CreateDynamicSteps();
                    var newStepCount = wizardSteps.Count;
                    
                    // ? If step count changed, we need to ensure we're going to the correct next step
                    if (newStepCount != oldStepCount)
                    {
                        System.Diagnostics.Debug.WriteLine($"Step count changed from {oldStepCount} to {newStepCount}");
                        // The next step should be the Caller step, which should be at index 2
                        // Find the correct index for the Caller step
                        var callerStepIndex = wizardSteps.FindIndex(s => s is CallerSetupWizardStep);
                        if (callerStepIndex != -1)
                        {
                            currentStepIndex = callerStepIndex - 1; // Will be incremented below
                            System.Diagnostics.Debug.WriteLine($"Adjusted currentStepIndex to {currentStepIndex} to navigate to Caller");
                        }
                    }
                }

                // Move to next step
                if (currentStepIndex < wizardSteps.Count - 1)
                {
                    currentStepIndex++;
                    System.Diagnostics.Debug.WriteLine($"Moved to step index {currentStepIndex} of {wizardSteps.Count}");
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
            finally
            {
                isNavigating = false;
            }
        }

        /// <summary>
        /// Navigates to the previous wizard step
        /// </summary>
        public async Task GoToPreviousStep()
        {
            // Prevent concurrent navigation
            if (isNavigating) return;
            
            try
            {
                isNavigating = true;
                
                if (currentStepIndex > 0)
                {
                    var currentStep = GetCurrentStep();
                    System.Diagnostics.Debug.WriteLine($"Going back from: {currentStep?.GetType().Name} (index {currentStepIndex})");

                    currentStepIndex--;
                    
                    var previousStep = GetCurrentStep();
                    System.Diagnostics.Debug.WriteLine($"Going back to: {previousStep?.GetType().Name} (index {currentStepIndex})");
                    
                    // ? If we're going back to extension selection, check if we need to adjust the current step index
                    // This handles cases where dynamic steps were added/removed and the index needs adjustment
                    if (previousStep is ExtensionSelectionWizardStep)
                    {
                        System.Diagnostics.Debug.WriteLine("Went back to extension selection step - dynamic steps may be rebuilt on next forward navigation");
                        // Don't remove steps here - let the next forward navigation handle the rebuild
                    }
                    
                    await ShowCurrentStep();
                }
            }
            finally
            {
                isNavigating = false;
            }
        }

        /// <summary>
        /// Removes dynamically created steps
        /// </summary>
        private void RemoveDynamicSteps()
        {
            System.Diagnostics.Debug.WriteLine($"Removing dynamic steps. Current step count: {wizardSteps.Count}");
            
            var staticSteps = wizardSteps.Where(step => 
                step is WelcomeWizardStep || 
                step is ExtensionSelectionWizardStep || 
                step is CallerSetupWizardStep || 
                step is CompletionWizardStep).ToList();

            // ? Adjust current step index if we're beyond the static steps
            var staticStepCount = staticSteps.Count;
            if (currentStepIndex >= staticStepCount)
            {
                // If we're currently on a dynamic step, move back to the last static step (usually completion or caller)
                currentStepIndex = Math.Min(currentStepIndex, staticStepCount - 1);
                System.Diagnostics.Debug.WriteLine($"Adjusted current step index to {currentStepIndex} after removing dynamic steps");
            }

            wizardSteps.Clear();
            wizardSteps.AddRange(staticSteps);
            
            System.Diagnostics.Debug.WriteLine($"After removing dynamic steps. Step count: {wizardSteps.Count}, Current index: {currentStepIndex}");
        }

        /// <summary>
        /// Shows the current wizard step
        /// </summary>
        private async Task ShowCurrentStep()
        {
            var currentStep = GetCurrentStep();
            if (currentStep != null && wizardWindow != null)
            {
                System.Diagnostics.Debug.WriteLine($"Displaying step: {currentStep.GetType().Name}");
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