using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.Excel;
using iTextSharp.text.pdf;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.DC3;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.DTOs.UserTestEvidence;
using RH_CM.Service.SQLSMS;
using System.Reflection.PortableExecutable;

namespace RH_CM.Service.DC3Service
{

    public class DC3Service
    {
        private readonly UnitOfWork _unitOfWork;

        public DC3Service(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ExcelExportDTOs> GetDC3File(int ControlNumber,string Course, string CompletedDate, string action)
        {
            ExcelExportDTOs excelExport = new();

            string[] date = (CompletedDate ?? string.Empty).Split(' ');
            if (date.Length < 3 || !int.TryParse(date[1], out int day) || !int.TryParse(date[2], out int year))
            {
                return excelExport;
            }
            string month = date[0];

            var parameters = new Dictionary<string, object>
            {
                { "@pControlNumber", ControlNumber },
                { "@pCourseName", Course }
            };

            List<DC3DTOs> answer = await _unitOfWork.ExecuteStoredProcedureToListAsync<DC3DTOs>("[sp_DC3Service_Get]", parameters);

            DC3DTOs? dC3DTOs = answer.FirstOrDefault();
            if (dC3DTOs == null)
            {
                return excelExport;
            }

            string templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "TemplateDC3.pdf");

            byte[] fileBytes;

            using (var templateStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var outputStream = new MemoryStream())
            {
                var reader = new PdfReader(templateStream);
                var stamper = new PdfStamper(reader, outputStream);
                var formFields = stamper.AcroFields;

                // 🔹 Rellenar campos
                formFields.SetField("txtFullName", dC3DTOs.FullName);
                formFields.SetField("txtCurp", dC3DTOs.Curp);
                formFields.SetField("txtOcupationID", dC3DTOs.OcupationID);
                formFields.SetField("txtPosition", dC3DTOs.PositionName);
                formFields.SetField("txtThematicID", dC3DTOs.ThematicCode);
                formFields.SetField("txtCourse", dC3DTOs.CourseName);
                formFields.SetField("txtDescription1", $"Impartido el {day} de {month} del {year}, con una duracion de 00 hora(s) ");
                //formFields.SetField("txtDescription2", dC3DTOs.ThematicName);

                if (action == "Admin")
                {
                    stamper.FormFlattening = false;
                }
                else
                {
                    stamper.FormFlattening = true;
                }
                // 🔹 “Aplastar” el formulario para que no sea editable


                stamper.Close();
                reader.Close();

                fileBytes = outputStream.ToArray();
            }


            excelExport.File = fileBytes;
            excelExport.FullName = $"{dC3DTOs.FullName}-{dC3DTOs.CourseName}-DC3";



            return excelExport;

        }


    }
}
