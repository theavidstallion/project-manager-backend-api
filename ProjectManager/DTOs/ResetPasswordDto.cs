using System.ComponentModel.DataAnnotations;

namespace ProjectManager.DTOs
{
    public class ResetPasswordDto
    {
        public string UserId { get; set; }

        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        public string Token { get; set; }

    }
}
