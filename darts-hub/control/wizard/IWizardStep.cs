using Avalonia.Controls;
using darts_hub.model;
using System.Threading.Tasks;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Interface for wizard steps in the setup process
    /// </summary>
    public interface IWizardStep
    {
        /// <summary>
        /// The title of the wizard step
        /// </summary>
        string Title { get; }

        /// <summary>
        /// A description of what this step does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The icon name for this step (should match asset files)
        /// </summary>
        string IconName { get; }

        /// <summary>
        /// Indicates if this step can be skipped
        /// </summary>
        bool CanSkip { get; }

        /// <summary>
        /// Initializes the wizard step with the selected profile and managers
        /// </summary>
        /// <param name="profile">The selected profile</param>
        /// <param name="profileManager">The profile manager</param>
        /// <param name="configurator">The configurator</param>
        void Initialize(Profile profile, ProfileManager profileManager, Configurator configurator);

        /// <summary>
        /// Creates the UI content for this wizard step
        /// </summary>
        /// <returns>The control containing the step's UI</returns>
        Task<Control> CreateContent();

        /// <summary>
        /// Validates the current step configuration
        /// </summary>
        /// <returns>Validation result</returns>
        Task<WizardValidationResult> ValidateStep();

        /// <summary>
        /// Applies the configuration from this step
        /// </summary>
        Task ApplyConfiguration();

        /// <summary>
        /// Called when the step is being shown
        /// </summary>
        Task OnStepShown();

        /// <summary>
        /// Called when the step is being hidden
        /// </summary>
        Task OnStepHidden();

        /// <summary>
        /// Resets the step to its initial state
        /// </summary>
        Task ResetStep();
    }

    /// <summary>
    /// Result of wizard step validation
    /// </summary>
    public class WizardValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public WizardValidationResult(bool isValid, string errorMessage = "")
        {
            IsValid = isValid;
            ErrorMessage = errorMessage ?? "";
        }

        public static WizardValidationResult Success()
        {
            return new WizardValidationResult(true);
        }

        public static WizardValidationResult Error(string message)
        {
            return new WizardValidationResult(false, message);
        }
    }
}