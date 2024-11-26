using Microsoft.AspNetCore.Mvc;
using Umbraco.Forms.Core.Services;
using Umbraco.Forms.Core.Persistence.Dtos;
using cms24_delta_umbraco.Models;

namespace cms24_delta_umbraco.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UmbracoFormsApiController : ControllerBase
    {
        private readonly IFormService _formService;
        private readonly ILogger<UmbracoFormsApiController> _logger;
        private readonly IRecordService _recordService;

        public UmbracoFormsApiController(IFormService formService, IRecordService recordService, ILogger<UmbracoFormsApiController> logger)
        {
            _formService = formService;
            _recordService = recordService;
            _logger = logger;
        }

        [HttpGet("{formId}")]
        public IActionResult GetForm(string formId)
        {
            try
            {
                _logger.LogInformation($"Försöker hämta formulär med ID: {formId}");
                var guid = Guid.Parse(formId);

                var form = _formService.Get(guid);

                if (form != null)
                {
                    var formData = new
                    {
                        FormName = form.Name,
                        Fields = form.AllFields.Select(field => new
                        {
                            field.Id,
                            field.Alias,
                            field.Caption,
                            field.Mandatory,
                            field.RequiredErrorMessage
                        })
                    };

                    return Ok(formData);
                }
                else
                {
                    _logger.LogError("Form not found!");
                    return NotFound("Form not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ett fel inträffade vid hämtning av formuläret.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("submitForm")]
        public async Task<IActionResult> SubmitForm([FromBody] FormData formData)
        {
            try
            {
                var formId = Guid.Parse(formData.FormId);
                var form = _formService.Get(formId);

                
                var record = new Record
                {
                    Form = formId,
                    State = Umbraco.Forms.Core.Enums.FormState.Submitted,
                    RecordFields = new Dictionary<Guid, RecordField>()
                };

                if (form == null)
                {
                    _logger.LogError("Form with ID {FormId} not found.", formId);
                    return NotFound($"Form with ID {formId} not found.");
                }

                
                foreach (var field in form.AllFields)
                {
                    if (field.Alias == "name" && formData.Name != null)
                    {
                        record.RecordFields.Add(field.Id, new RecordField
                        {
                            FieldId = field.Id,
                            Alias = field.Alias,
                            DataTypeAlias = field.FieldTypeId.ToString(),
                            Field = field,
                            Values = new List<object> { formData.Name }
                        });
                    }
                    else if (field.Alias == "email" && formData.Email != null)
                    {
                        record.RecordFields.Add(field.Id, new RecordField
                        {
                            FieldId = field.Id,
                            Alias = field.Alias,
                            DataTypeAlias = field.FieldTypeId.ToString(),
                            Field = field,
                            Values = new List<object> { formData.Email }
                        });
                    }
                    else if (field.Alias == "message" && formData.Message != null)
                    {
                        record.RecordFields.Add(field.Id, new RecordField
                        {
                            FieldId = field.Id,
                            Alias = field.Alias,
                            DataTypeAlias = field.FieldTypeId.ToString(),
                            Field = field,
                            Values = new List<object> { formData.Message }
                        });
                    }
                    else if (field.Alias == "dataConsent")
                    {
                        record.RecordFields.Add(field.Id, new RecordField
                        {
                            FieldId = field.Id,
                            Alias = field.Alias,
                            DataTypeAlias = field.FieldTypeId.ToString(),
                            Field = field,
                            Values = new List<object> { formData.DataConsent.ToString() }
                        });
                    }
                }

                foreach (var field in record.RecordFields)
                {
                    _logger.LogInformation("Field ID: {FieldId}, Value: {Value}",
                        field.Key, string.Join(", ", field.Value.Values));
                }

                await _recordService.SubmitAsync(record, form);

                _logger.LogInformation("Form submitted successfully and saved to Umbraco.");

                return Ok("Form submitted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting form");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
