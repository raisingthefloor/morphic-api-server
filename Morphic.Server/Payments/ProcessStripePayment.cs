// Copyright 2020 Raising the Floor - International
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/GPII/universal/blob/master/LICENSE.txt
//
// The R&D leading to these results received funding from the:
// * Rehabilitation Services Administration, US Dept. of Education under 
//   grant H421A150006 (APCP)
// * National Institute on Disability, Independent Living, and 
//   Rehabilitation Research (NIDILRR)
// * Administration for Independent Living & Dept. of Education under grants 
//   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
// * European Union's Seventh Framework Programme (FP7/2007-2013) grant 
//   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
// * William and Flora Hewlett Foundation
// * Ontario Ministry of Research and Innovation
// * Canadian Foundation for Innovation
// * Adobe Foundation
// * Consumer Electronics Association Foundation

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Morphic.Server.Payments
{
    using Background;
    using Db;
    using Users;
    
    public class ProcessStripePayment : BackgroundJob
    {
        private readonly StripeSettings stripeSetting;
        private readonly ILogger<ProcessStripePayment> logger;
        private readonly Database db;
        
        public ProcessStripePayment(MorphicSettings morphicSettings, StripeSettings stripeSetting, ILogger<ProcessStripePayment> logger, Database db): base(morphicSettings)
        {
            this.stripeSetting = stripeSetting;
            this.logger = logger;
            this.db = db;
        }
        
        private Dictionary<string, string> StripeMetadata(User user)
        {
            return new Dictionary<string, string>
            {
                {"MorphicUserId", user.Id},
            };
        }

        private Dictionary<string, string> StripeMetadata(UserPaymentHistory userPaymentHistory)
        {
            return new Dictionary<string, string>
            {
                {"MorphicUserId", userPaymentHistory.UserId},
            };
        }

        [AutomaticRetry(Attempts = 20)]
        public async Task ProcessPayment(string transactionId, string? clientIp)
        {
            var transaction = await db.Get<PaymentTransaction>(transactionId);
            if (transaction == null)
            {
                throw new ProcessStripePaymentException($"No transaction found for id {transactionId}");
            }

            var user = await db.Get<User>(transaction.UserId);
            if (user == null)
            {
                throw new ProcessStripePaymentException($"User not found for id {transaction.UserId}");
            }

            try
            {
                // Find the user's payment history with us. If not found:
                //   assume we don't have this client in stripe, so create a new customer ID there.
                //   create a new payment history entry 
                var history = await db.Get<UserPaymentHistory>(h =>
                    h.UserId == transaction.UserId && h.Processor == PaymentProcessors.Stripe);
                if (history == null)
                {
                    var customerId = await CreateCustomer(user, transaction.TransactionKey);
                    history = new UserPaymentHistory(user, PaymentProcessors.Stripe);
                    history.ProcessorCustomerId = customerId;
                    await db.Save(history);
                }

                // If we're not using a previously saved ID for the credit card, create one in stripe and save
                // the ID for future reference.
                if (string.IsNullOrEmpty(transaction.CreditCardId))
                {
                    if (transaction.CreditCardInfo == null)
                    {
                        throw new ProcessStripePaymentException("CreditCardInfo and ccId can not both be empty");
                    }

                    var ccId = CreateCard(history, transaction.CreditCardInfo, transaction.TransactionKey);
                    history.CreditCardIdList ??= new List<string>();
                    history.CreditCardIdList.Add(ccId);
                    transaction.CreditCardId = ccId;
                    transaction.CreditCardInfo = null;
                    await db.Save(history);
                    await db.Save(transaction);
                }

                // Make the payment
                var paymentId = MakePayment(user, history,
                    transaction.CreditCardId,
                    transaction.Amount,
                    transaction.Currency,
                    transaction.TransactionKey);
                history.TransactionIdList ??= new List<TransactionHistory>();
                history.TransactionIdList.Add(new TransactionHistory(paymentId, clientIp));
                await db.Save(history);
            }
            catch (ProcessStripePaymentException e)
            {
                logger.LogError("Could not process Payment. Aborting. {Exception}", e);
            }
        }

        #region Stripe Helpers
        public async Task<string> CreateCustomer(User user, string transactionKey)
        {
            var createOptions = new CustomerCreateOptions
            {
                Description = "MorphicLite Customer",
                Email = user.Email.PlainText,
                Metadata = StripeMetadata(user),
            };
            var service = new CustomerService();
            var requestOptions = stripeSetting.StripeRequestOptions($"cus_{transactionKey}");
            try
            {
                var customer = await service.CreateAsync(createOptions, requestOptions);
                return customer.Id;
            }
            catch (StripeException e)
            {
                logger.LogError("Error creating customer: {Type} {Code} {Message}",
                    e.StripeError.Type, e.StripeError.Code, e.StripeError.Message);
                throw new ProcessStripePaymentException("Stripe Exception", e);
            }
        }

        public string CreateCard(UserPaymentHistory userPaymentHistory, CreditCardInfo ccInfo, string transactionKey)
        {
            DateTime expDate;
            if (!DateTime.TryParse(ccInfo.ExpirationDate.PlainText, out expDate))
            {
                throw new ProcessStripePaymentException($"Could not parse {ccInfo.ExpirationDate.PlainText} to datetime");
            }

            var source = new CardCreateNestedOptions {
                Number = ccInfo.Number.PlainText,
                Cvc = ccInfo.Cvv.PlainText,
                ExpMonth = expDate.Month,
                ExpYear = expDate.Year,
                AddressZip = ccInfo.ZipCode.PlainText,
            };
            var options = new CardCreateOptions
            {
                Source = source,
                Metadata = StripeMetadata(userPaymentHistory),
            };
            var service = new CardService();
            var requestOptions = stripeSetting.StripeRequestOptions($"cc_{transactionKey}");
            try
            {
                var response = service.Create(userPaymentHistory.ProcessorCustomerId, options, requestOptions);
                return response.Id;
            }
            catch (StripeException e)
            {
                logger.LogError("Error creating credit card: {Type} {Code} {Message}",
                    e.StripeError.Type, e.StripeError.Code, e.StripeError.Message);
                throw new ProcessStripePaymentException("Stripe Exception", e);
            }

        }
        
        public string MakePayment(User user, UserPaymentHistory userPaymentHistory, string source, long amount, string currency,
            string transactionKey)
        {
            var options = new ChargeCreateOptions
            {
                Customer = userPaymentHistory.ProcessorCustomerId,
                Amount = amount,
                Currency = currency,
                Source = source,
                Description = "My First Test Charge (created for API docs)",
                ReceiptEmail = user.Email.PlainText,
                Metadata = StripeMetadata(user),
            };
            var service = new ChargeService();
            var requestOptions = stripeSetting.StripeRequestOptions($"pay_{transactionKey}");
            try
            {
                var response = service.Create(options, requestOptions);
                return response.Id;
            }
            catch (StripeException e)
            {
                logger.LogError("Error creating payment: {Type} {Code} {Message}",
                    e.StripeError.Type, e.StripeError.Code, e.StripeError.Message);
                throw new ProcessStripePaymentException("Stripe Exception", e);
            }
        }
        
        #endregion

        public class ProcessStripePaymentException : MorphicServerException
        {
            public ProcessStripePaymentException(string message) : base(message)
            {
            }
            public ProcessStripePaymentException(string message, Exception e) : base(message, e)
            {
            }
        }
    }
}