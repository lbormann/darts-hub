using Avalonia.Controls;
using Avalonia.Interactivity;
using darts_hub.control.wizard;
using MsBox.Avalonia;
using System;
using System.Threading.Tasks;

namespace darts_hub.control.wizard
{
    public partial class WizardWindow : Window
    {
        private SetupWizardManager wizardManager;
        
        public WizardWindow() : this(null) { }

        public WizardWindow(SetupWizardManager manager)
        {
            InitializeComponent();
            wizardManager = manager;
        }

        /// <summary>
        /// Displays the specified wizard step
        /// </summary>
        public async Task DisplayStep(IWizardStep step)
        {
            if (step == null) return;

            SetLoading(true, "Loading step...");

            try
            {
                // Update header information
                StepTitle.Text = step.Title;
                StepDescription.Text = step.Description;
                
                // Update step counter
                if (wizardManager != null)
                {
                    StepCounter.Text = $"Step {wizardManager.GetCurrentStepNumber()} of {wizardManager.GetTotalSteps()}";
                    WizardProgressBar.Value = wizardManager.GetProgress() * 100;
                }

                // Update navigation buttons
                UpdateNavigationButtons();

                // Show/hide skip button
                SkipButton.IsVisible = step.CanSkip;

                // Load step icon if available
                try
                {
                    if (!string.IsNullOrEmpty(step.IconName))
                    {
                        StepIcon.Source = new Avalonia.Media.Imaging.Bitmap(
                            Avalonia.Platform.AssetLoader.Open(new Uri($"avares://darts-hub/Assets/{step.IconName}.png")));
                    }
                }
                catch (Exception)
                {
                    // Use default icon if step icon not found
                    StepIcon.Source = new Avalonia.Media.Imaging.Bitmap(
                        Avalonia.Platform.AssetLoader.Open(new Uri("avares://darts-hub/Assets/darts.png")));
                }

                // Create and display step content
                var content = await step.CreateContent();
                StepContentPresenter.Content = content;

                // Notify step it's being shown
                await step.OnStepShown();

                // Scroll to top
                ContentScrollViewer.Offset = new Avalonia.Vector(0, 0);
            }
            catch (Exception ex)
            {
                await ShowValidationError($"Failed to load wizard step: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        /// <summary>
        /// Shows a validation error to the user
        /// </summary>
        public async Task ShowValidationError(string message)
        {
            ErrorMessage.Text = message;
            ErrorOverlay.IsVisible = true;
        }

        /// <summary>
        /// Updates the state of navigation buttons
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (wizardManager == null) return;

            PreviousButton.IsEnabled = wizardManager.CanGoPrevious();
            
            if (wizardManager.IsLastStep())
            {
                NextButton.Content = "Finish";
            }
            else
            {
                NextButton.Content = "Next →";
            }
        }

        /// <summary>
        /// Sets the loading state
        /// </summary>
        private void SetLoading(bool isLoading, string message = "Processing...")
        {
            LoadingOverlay.IsVisible = isLoading;
            LoadingText.Text = message;
        }

        // Event handlers
        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (wizardManager != null)
            {
                SetLoading(true, "Processing configuration...");
                try
                {
                    await wizardManager.GoToNextStep();
                }
                catch (Exception ex)
                {
                    await ShowValidationError($"An error occurred: {ex.Message}");
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (wizardManager != null)
            {
                SetLoading(true, "Loading previous step...");
                try
                {
                    await wizardManager.GoToPreviousStep();
                }
                catch (Exception ex)
                {
                    await ShowValidationError($"An error occurred: {ex.Message}");
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        private async void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            var messageBox = MessageBoxManager
                .GetMessageBoxStandard("Skip Step", 
                    "Are you sure you want to skip this configuration step? You can always configure this later in the settings.",
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                    MsBox.Avalonia.Enums.Icon.Question);
            
            var result = await messageBox.ShowWindowDialogAsync(this);
            
            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                if (wizardManager != null)
                {
                    SetLoading(true, "Skipping step...");
                    try
                    {
                        await wizardManager.GoToNextStep();
                    }
                    catch (Exception ex)
                    {
                        await ShowValidationError($"An error occurred: {ex.Message}");
                    }
                    finally
                    {
                        SetLoading(false);
                    }
                }
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var messageBox = MessageBoxManager
                .GetMessageBoxStandard("Cancel Setup", 
                    "Are you sure you want to cancel the setup wizard? Your configuration will not be saved.",
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                    MsBox.Avalonia.Enums.Icon.Warning);
            
            var result = await messageBox.ShowWindowDialogAsync(this);
            
            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                wizardManager?.CancelWizard();
            }
        }

        private void ErrorOkButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorOverlay.IsVisible = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}