using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace SycamoreHockeyLeaguePortal.Models.InputForms
{
    public class Schedule_UploadForm
    {
        public int Season { get; set; }
        public IFormFile File { get; set; }
    }
}
