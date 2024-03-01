﻿using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyBGList.Swagger
{
    public class UsernameRequestFilter : IRequestBodyFilter
    {
        public void Apply(OpenApiRequestBody requestBody,
            RequestBodyFilterContext context)
        {
            var fieldName = "username";

            if (context.BodyParameterDescription.Name
                .Equals(fieldName,
                    StringComparison.OrdinalIgnoreCase)
                || context.BodyParameterDescription.Type
                .GetProperties().Any(p => p.Name
                    .Equals(fieldName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                requestBody.Description +=
                    "\nIMPORTANT: Be sure to remember your username, " +
                    "as you will need it to perform the login!";
            }
        }
    }
}
