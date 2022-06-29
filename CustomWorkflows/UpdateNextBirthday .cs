using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Messages;

namespace CustomWorkflows 
{
    public class UpdateNextBirthday : CodeActivity
    {
        [RequiredArgument]
        [Input("Update Next Birthdate for")]
        [ReferenceTarget("contact")]
        public InArgument<EntityReference> Contact { get; set; }

        //[RequiredArgument]
        //[Output("bcd")]
        //public OutArgument<string> bcd { get; set; }

        protected override void Execute(CodeActivityContext executionContext) {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            //Create Organization Service
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.InitiatingUserId);

            //get contact id
            var contactObj = this.Contact.Get(executionContext);

            //create the request
           Entity entity =  service.Retrieve("contect",contactObj.Id,new Microsoft.Xrm.Sdk.Query.ColumnSet("birthdate"));

            RetrieveRequest request = new RetrieveRequest() {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet(new string[] { "birthdate" }),
                Target = new EntityReference(contactObj.LogicalName, contactObj.Id)
            };

            Entity dummyEntity = (Entity)((RetrieveResponse)service.Execute(request)).Entity;

            DateTime? birthdate = null;

            if (entity.Attributes.Contains("birthdate")) {
                birthdate = entity.GetAttributeValue<DateTime>("birthdate");
            }

            if (birthdate == null) {
                return;
            }

            DateTime nextBirthDate = CalculateNextBirthdayDate(birthdate.Value);

            Entity updateEntity = new Entity(contactObj.LogicalName) { 
                Id =  contactObj.Id,
                ["new_nextbirthday"] = nextBirthDate
            };
            service.Update(updateEntity);
        }

        private DateTime CalculateNextBirthdayDate(DateTime birthdate) {
            DateTime nextBirthdayDate = new DateTime(birthdate.Year,birthdate.Month,birthdate.Day);
            //Check to see if birthdate occurred on leap year
            bool leapYearAdjust = false;
            if (nextBirthdayDate.Month == 2 && nextBirthdayDate.Day == 29) {
                //sanity Check, was that year a leap year
                if (DateTime.IsLeapYear(nextBirthdayDate.Year))
                {
                    //check if current year is leap year
                    if (!DateTime.IsLeapYear(DateTime.Now.Year)) {
                        nextBirthdayDate.AddDays(1);
                        leapYearAdjust = true;
                    }
                }
                else {
                    throw new Exception("UpdateNextBirthday : birthdate is not correctly set", new ArgumentException("birthdate"));
                }
            };
            nextBirthdayDate.AddYears(DateTime.Now.Year - nextBirthdayDate.Year);

            //Check to see if leapyear was adjusted
            if (leapYearAdjust && DateTime.IsLeapYear(nextBirthdayDate.Year)) {
                nextBirthdayDate.AddDays(-1);
            }
            return nextBirthdayDate;
        }
    }
}
