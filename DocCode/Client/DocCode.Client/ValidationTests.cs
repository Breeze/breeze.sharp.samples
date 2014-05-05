using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Breeze.Sharp;
using Breeze.Sharp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Northwind.Models;

namespace Test_NetClient
{
    [TestClass]
    public class ValidationTests
    {
        private String _serviceName;

        [TestInitialize]
        public void TestInitializeMethod()
        {
            Configuration.Instance.ProbeAssemblies(typeof(Customer).Assembly);
            _serviceName = "http://localhost:56337/breeze/Northwind/";
        }

        /// <summary>
        /// Does validate on attach by default.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DoesValidateOnAttachByDefault()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var customer = new Customer();
            
            var validationErrors = customer.EntityAspect.ValidationErrors;
            Assert.IsFalse(validationErrors.Any(), "Should be no validation errors before attach.");

            // attach triggers entity validation by default
            manager.AttachEntity(customer);

            Assert.IsTrue(validationErrors.Any(ve => ve.Message.Contains("CompanyName") && ve.Message.Contains("required")), "Should be a validation error stating CompanyName is required.");
        }

        /// <summary>
        /// Does NOT validate on attach when that ValidationOption (OnAttach) is turned off.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DoesNotValidateOnAttachWhenOptionIsOff()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            // change the default options, turning off "OnAttach"
            var valOpts = new ValidationOptions { ValidationApplicability = ValidationApplicability.OnPropertyChange | ValidationApplicability.OnSave };
            
            // reset manager's options
            manager.ValidationOptions = valOpts;

            var customer = new Customer();
            manager.AttachEntity(customer);

            var validationErrors = customer.EntityAspect.ValidationErrors;
            Assert.IsFalse(validationErrors.Any(), "Should be no validation errors even though CompanyName is required.");
        }

        /// <summary>
        /// Make custom ValidationOptions the default for all future managers.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public void SetCustomValidationOptionsAsDefault()
        {
            // change the default options, turning off "OnAttach"
            var valOpts = new ValidationOptions { ValidationApplicability = ValidationApplicability.OnPropertyChange | ValidationApplicability.OnSave };
          
          var oldValOpts = ValidationOptions.Default;
          try {
            // make custom ValidationOptions the default for all future managers
            ValidationOptions.Default = valOpts;

            var manager = new EntityManager(_serviceName); // new empty EntityManager
            Assert.AreEqual(valOpts, manager.ValidationOptions);
          }
          finally {
            ValidationOptions.Default = oldValOpts;
          }
        }

        [TestMethod]
        public async Task ManualValidationAndClearingOfErrors()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var newCustomer = new Customer();
            // attach triggers entity validation by default
            manager.AttachEntity(newCustomer);

            // validate an individual entity at any time
            var results = newCustomer.EntityAspect.Validate();
            if (results.Any()) {/* do something about errors */}
            Assert.IsTrue(results.Any(ve => ve.Message.Contains("CompanyName") && ve.Message.Contains("required")), "Should be a validation error stating CompanyName is required.");

            // remove all current errors from the collection
            newCustomer.EntityAspect.ValidationErrors.Clear();
            Assert.IsFalse(newCustomer.EntityAspect.ValidationErrors.Any(), "Should be no validation errors after clearing them.");

            // validate a specific property at any time
            var dp = newCustomer.EntityAspect.EntityType.GetDataProperty("CompanyName");
            results = newCustomer.EntityAspect.ValidateProperty(dp);
            if (results.Any()) { /* do something about errors */}
            Assert.IsTrue(results.Any(ve => ve.Message.Contains("CompanyName") && ve.Message.Contains("required")), "Should be a validation error stating CompanyName is required.");

            // remove a specific error
            var specificError = results.First(ve => ve.Context.PropertyPath == "CompanyName");
            newCustomer.EntityAspect.ValidationErrors.Remove(specificError);
            Assert.IsFalse(newCustomer.EntityAspect.ValidationErrors.Any(), "Should be no validation errors after clearing a specific one.");

            // clear server errors
            var errors = newCustomer.EntityAspect.ValidationErrors;
            errors.ForEach(ve =>
                               {
                                   if (ve.IsServerError) errors.Remove(ve);
                               });

        }

        /// <summary>
        /// Add required validator to the Country property of a Customer entity.
        /// </summary>
        [TestMethod]
        public async Task AddRequiredValidator()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            // Add required validator to the Country property of a Customer entity
            var customerType = manager.MetadataStore.GetEntityType(typeof(Customer)); //get the Customer type
            var countryProp = customerType.GetProperty("Country"); //get the property definition to validate

            var validators = countryProp.Validators; // get the property's validator collection
            Assert.IsFalse(validators.Any(v => v == new RequiredValidator()), "Should be no required validators on Customer.Country.");

            try
            {
                validators.Add(new RequiredValidator()); // add a new required validator instance 
                Assert.IsTrue(validators.Any(), "Should now have a required validator on Customer.Country.");

                // create a new customer setting the required CompanyName
                var customer = new Customer { CompanyName = "zzz" };
                var validationErrors = customer.EntityAspect.ValidationErrors;
                Assert.IsFalse(validationErrors.Any(), "Should be no validation errors prior to attach");

                // attach triggers entity validation by default
                manager.AttachEntity(customer);
                // customer is no longer valid since Country was never set
                Assert.IsTrue(validationErrors.Any(ve => ve.Message.Contains("Country") && ve.Message.Contains("required")), "Should be a validation error stating Country is required.");
            }
            finally
            {
                Assert.IsTrue(validators.Remove(new RequiredValidator()));
            }
        }

        /// <summary>
        /// Customer can be in Canada (before applying custom CountryIsUsValidator)
        /// </summary>
        [TestMethod]
        public async Task CustomerCanBeInCanada()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var customer = new Customer {CompanyName="zzz", ContactName = "Wayne Gretzky", Country = "Canada"};

            // force validation of unattached customer
            var errors = customer.EntityAspect.Validate();

            // Ok for customer to be in Canada
            Assert.IsFalse(errors.Any(), "Should be no validation errors.");
        }
        
        /// <summary>
        /// Customer must be in US (after applying custom CountryIsUsValidator)
        /// </summary>
        [TestMethod]
        public async Task CustomerMustBeInUs()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var customer = new Customer { CompanyName = "zzz", ContactName = "Shania Twain" };
            manager.AttachEntity(customer);

            try
            {
                // add the US-only validator
                customer.EntityAspect.EntityType.GetProperty("Country")
                        .Validators.Add(new CountryIsUsValidator());

                // non-US customers no longer allowed 
                customer.Country = "CA";
                var validationErrors = customer.EntityAspect.ValidationErrors;
                Assert.IsTrue(validationErrors.Any(ve => ve.Message.Contains("Country must start with 'US'")), "Should be a validation error stating Country must start with 'US'.");

                // back in the USA 
                customer.Country = "USA";
                Assert.IsFalse(validationErrors.Any(), "Should be no validation errors.");
            }
            finally
            {
                Assert.IsTrue(customer.EntityAspect.EntityType.GetProperty("Country")
                        .Validators.Remove(new CountryIsUsValidator()));
            }
        }

        /// <summary>
        /// Employee must be from US (after applying custom CountryIsUsValidator).
        /// Illustrates reuse of validator on different entity type.
        /// </summary>
        [TestMethod]
        public async Task EmployeeMustBeFromUs()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var employee = new Employee { FirstName = "Bill", LastName = "Gates" };
            manager.AttachEntity(employee);

            try
            {
                // add the US-only validator
                employee.EntityAspect.EntityType.GetProperty("Country")
                        .Validators.Add(new CountryIsUsValidator());

                // non-US customers no longer allowed 
                employee.Country = "Canada";
                var validationErrors = employee.EntityAspect.ValidationErrors;
                Assert.IsTrue(validationErrors.Any(ve => ve.Message.Contains("Country must start with 'US'")), "Should be a validation error stating Country must start with 'US'.");

                // back in the USA 
                employee.Country = "USA";
                Assert.IsFalse(validationErrors.Any(), "Should be no validation errors.");
            }
            finally
            {
                Assert.IsTrue(employee.EntityAspect.EntityType.GetProperty("Country")
                        .Validators.Remove(new CountryIsUsValidator()));                
            }
        }

        /// <summary>
        /// US Customer must have valid zip code
        /// </summary>
        [TestMethod]
        public async Task USCustomerMustHaveValidZipCode()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var customerType = manager.MetadataStore.GetEntityType(typeof(Customer)); //get the Customer type

            try
            {
                // add US zip code validator to the entity (not to a property)
                customerType.Validators.Add(new ZipCodeValidator());

                var customer = new Customer { CustomerID = Guid.NewGuid(), CompanyName = "Boogaloo Board Games" };
                customer.Country = "USA";
                customer.PostalCode = "N2L 3G1"; // a Canadian postal code
                manager.AddEntity(customer);
                // force validation of  customer
                var errors = customer.EntityAspect.Validate();

                Assert.IsTrue(errors.Any(), String.Format("should have 1 error: {0}", errors.First().Message));

                customer.Country = "Canada";

                errors = customer.EntityAspect.Validate();

                Assert.IsFalse(errors.Any(), String.Format("should have no errors"));
            }
            finally
            {
                Assert.IsTrue(customerType.Validators.Remove(new ZipCodeValidator()));
            }
        }

        /// <summary>
        /// Remove a rule ... and an error
        /// </summary>
        [TestMethod]
        public async Task RemoveRuleAndError()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var customerType = manager.MetadataStore.GetEntityType(typeof(Customer)); //get the Customer type
            
            var alwaysWrongValidator = new AlwaysWrongValidator();
            var validators = customerType.Validators;

            try
            {
                // add alwaysWrong to the entity (not to a property)
                validators.Add(alwaysWrongValidator);

                var customer = new Customer { CompanyName = "Presumed Guilty"};

                // Attach triggers entity validation by default
                manager.AttachEntity(customer);

                var errmsgs = customer.EntityAspect.ValidationErrors;
                Assert.IsTrue(errmsgs.Any(), String.Format("should have 1 error: {0}", errmsgs.First().Message));

                // remove validation rule
                Assert.IsTrue(validators.Remove(alwaysWrongValidator));

                // Clear out the "alwaysWrong" error
                // Must do manually because that rule is now gone
                // and, therefore, can't cleanup after itself
                customer.EntityAspect.ValidationErrors.RemoveKey(ValidationError.GetKey(alwaysWrongValidator));

                customer.EntityAspect.Validate(); // re-validate

                Assert.IsFalse(errmsgs.Any(), "should have no errors");
            }
            finally
            {
                validators.Remove(alwaysWrongValidator);
            }
        }

        /// <summary>
        /// Add and remove a (fake) ValidationError
        /// </summary>
        [TestMethod]
        public async Task AddRemoveValidationError()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var customer = new Customer { CompanyName = "Presumed Guilty"};
            manager.AttachEntity(customer);

            var errmsgs = customer.EntityAspect.ValidationErrors;
            Assert.IsFalse(errmsgs.Any(), "should have no errors at start");

            // create a fake error
            var fakeError = new ValidationError( 
                new AlwaysWrongValidator(),     // the marker validator
                new ValidationContext(customer),// validation context
                "You were wrong this time!"     // error message
            );

            // add the fake error
            errmsgs.Add(fakeError);

            Assert.IsTrue(errmsgs.Any(), String.Format("should have 1 error after add: {0}", errmsgs.First().Message));               

            // Now remove that error
            errmsgs.Remove(fakeError);

            customer.EntityAspect.Validate(); // re-validate

            Assert.IsFalse(errmsgs.Any(), "should have no errors after remove");
        }

        /// <summary>
        /// Customize message templates
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CustomizeMessageString()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var customer = new Customer();
            var vr = new RequiredValidator().WithMessage("Dude! The {0} is really required ... seriously ... as in mandatory");

            var companyNameProp = customer.EntityAspect.EntityType.GetDataProperty("CompanyName");
            var context = new ValidationContext(customer, companyNameProp, null);
            var error = vr.Validate(context);
            Assert.IsTrue(error.Message.Contains("CompanyName") && error.Message.Contains("Dude"), "should be an error containing 'Dude'");
        }

        /// <summary>
        /// US Customer must have valid zip code
        /// </summary>
        [TestMethod]
        public async Task EmployeeMustHaveValidUSPhone()
        {
            var manager = new EntityManager(_serviceName); // new empty EntityManager
            await manager.FetchMetadata(); // required before creating a new entity

            var employeeType = manager.MetadataStore.GetEntityType(typeof(Employee)); //get the Employee type

            var phoneValidator = new PhoneNumberValidator();
            // or the hard way ... :)
            // var phoneValidator = new RegexValidator(@"^((\([2-9]\d{2}\) ?)|([2-9]\d{2}[-.]))\d{3}[-.]\d{4}$", "phone number"); // email pattern
            try
            {
                // add regex validator that validates emails to the HomePhone property
                employeeType.GetProperty("HomePhone").Validators.Add(phoneValidator); 

                var employee = new Employee { FirstName = "Jane", LastName = "Doe" };
                employee.HomePhone = "N2L 3G1"; // a bad phone number

                // force validation of unattached employee
                manager.AttachEntity(employee);
                var errors = employee.EntityAspect.ValidationErrors;

                Assert.IsTrue(errors.Any(), String.Format("should have 1 error: {0}", errors.First().Message));

                employee.HomePhone = "510-555-1111";

                Assert.IsFalse(errors.Any(), String.Format("should have no errors"));
            }
            finally
            {
                Assert.IsTrue(employeeType.GetProperty("HomePhone").Validators.Remove(phoneValidator));
            }
        }



        #region /****** CUSTOM VALIDATORS *******/

        /// <summary>
        /// Custom validator that ensures "Country" is US
        /// </summary>
        public class CountryIsUsValidator : Validator
        {
            public CountryIsUsValidator()
            {
                LocalizedMessage = new LocalizedMessage("{0} must start with 'US', '{1}' is not valid");
            }

            protected override bool ValidateCore(ValidationContext context)
            {
                var value = (String) context.PropertyValue;
                if (value == null) return true; // '== null' matches null and empty string
                return value.ToUpper().StartsWith("US");
            }

            public override string GetErrorMessage(ValidationContext validationContext)
            {
                return LocalizedMessage.Format(validationContext.Property.DisplayName, validationContext.PropertyValue);
            }
        }

        /// <summary>
        /// Custom validator that checks for a non-zero Id
        /// </summary>
        public class NonZeroIdValidator : Validator
        {
            public NonZeroIdValidator()
            {
                LocalizedMessage = new LocalizedMessage("{0} is required");
            }

            protected override bool ValidateCore(ValidationContext context)
            {
                var value = (int) context.PropertyValue;
                return value != 0;
            }

            public override string GetErrorMessage(ValidationContext validationContext)
            {
                return LocalizedMessage.Format(validationContext.Property.DisplayName);
            }
        }

        /// <summary>
        /// Custom validator that checks for a valid zip code in the US
        /// </summary>
        public class ZipCodeValidator : Validator
        {
            public ZipCodeValidator()
            {
                LocalizedMessage = new LocalizedMessage("{0} is not a valid US zip code");
            }

            protected override bool ValidateCore(ValidationContext context)
            {
                // This validator only validates US Zip Codes.
                var entity = context.Entity;
                if (entity.GetPropValue<string>("Country") == "USA")
                {
                    var postalCode = entity.GetPropValue<string>("PostalCode");
                    context.PropertyValue = postalCode;
                    return IsValidZipCode(postalCode);
                }
                return true;
            }

            private static bool IsValidZipCode(string postalCode)
            {
                const string pattern = @"/^\d{5}([\-]\d{4})?$/";
                return Regex.IsMatch(postalCode, pattern);
            }

            public override string GetErrorMessage(ValidationContext validationContext)
            {
                return LocalizedMessage.Format(validationContext.PropertyValue);
            }
        }

        /// <summary>
        /// Custom validator that is always wrong
        /// </summary>
        public class AlwaysWrongValidator : Validator
        {
            public AlwaysWrongValidator()
            {
                LocalizedMessage = new LocalizedMessage("You are always wrong!");
            }

            protected override bool ValidateCore(ValidationContext context)
            {
                return false;
            }

            public override string GetErrorMessage(ValidationContext validationContext)
            {
                return LocalizedMessage.ToString();
            }
        }

        #endregion

    }
}
