using System.Diagnostics;

namespace Ortho_xact_api.DTO
{
    public static class HandleExe
    {
        public static string RunMyExe(string SalesOrder,int docType)
        {

            if (long.TryParse(SalesOrder, out long numericOrder))
            {
                // Format to 15 digits with leading zeros
                SalesOrder = numericOrder.ToString("D15");
            }

            string exePath = @"C:\Codex\Report\OrthoReport.exe";
           //string exePath = @"C:\DEVELOPMENT\OrthoReport\bin\Release\OrthoReport.exe"; // Full path to the exe
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = SalesOrder+","+docType.ToString(), // Optional: pass arguments here
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return string.IsNullOrWhiteSpace(error) ? output : $"Error: {error}";
        }

    }
}
