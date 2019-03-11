namespace WadeCoin.Core.Validation
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; }

        public ValidationResult(bool isValid, string error = null)
        {
            IsValid = isValid;
            Error = error;
        }

        public static ValidationResult Valid() => new ValidationResult(isValid:true);
        public static ValidationResult Invalid(string error) => new ValidationResult(isValid: false, error: error);

        public static implicit operator bool(ValidationResult result) => result.IsValid;
        public static implicit operator ValidationResult(bool isValid) => new ValidationResult(isValid);
    }
}