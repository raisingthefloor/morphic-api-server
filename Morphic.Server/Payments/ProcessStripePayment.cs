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
using System.Net;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Morphic.Server.Db;
using Morphic.Server.Users;
using Stripe;

namespace Morphic.Server.Payments
{
    using Background;
    public class ProcessStripePayment : BackgroundJob
    {
        private readonly StripeSettings stripeSetting;
        private readonly ILogger<ProcessStripePayment> logger;
        private readonly Database db;
        
        protected ProcessStripePayment(MorphicSettings morphicSettings, StripeSettings stripeSetting,
            ILogger<ProcessStripePayment> logger, Database db) : base(morphicSettings)
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

        private Dictionary<string, string> StripeMetadata(History history)
        {
            return new Dictionary<string, string>
            {
                {"MorphicUserId", history.UserId},
            };
        }

        [AutomaticRetry(Attempts = 20)]
        public async Task ProcessPayment(string transactionId, string? clientIp)
        {
            var transaction = await db.Get<Transaction>(transactionId);
            if (transaction == null)
            {
                throw new ProcessStripePaymentException($"No transaction found for id {transactionId}");
            }

            var user = await db.Get<User>(transaction.UserId);
            if (user == null)
            {
                throw new ProcessStripePaymentException($"User not found for id {transaction.UserId}");
            }
            
            var requestOptions = stripeSetting.StripeRequestOptions(transaction.IdempotencyKey);
            
            var history = await db.Get<History>(h =>
                h.UserId == transaction.UserId && h.Processor == PaymentProcessors.Stripe);
            if (history == null)
            {
                history = new History(user, PaymentProcessors.Stripe);
                // TODO need to create customer first
                var customerId = await CreateCustomer(user, requestOptions);
                history.ProcessorCustomerId = customerId;
                await db.Save(history);
            }

            if (string.IsNullOrEmpty(transaction.CreditCardId))
            {
                if (transaction.CreditCardInfo == null)
                {
                    throw new ProtocolViolationException("CreditCardInfo and ccId can not both be empty");
                }
                var ccId = CreateCard(history, transaction.CreditCardInfo, requestOptions);
                history.CreditCardIdList ??= new List<string>();
                history.CreditCardIdList.Add(ccId);
                transaction.CreditCardId = ccId;
                transaction.CreditCardInfo = null;
                await db.Save(history);
                await db.Save(transaction);
            }

            var paymentId = MakePayment(user, history,
                transaction.CreditCardId,
                transaction.Amount,
                transaction.Currency,
                requestOptions);
            history.TransactionIdList ??= new List<TransactionHistory>();
            history.TransactionIdList.Add(new TransactionHistory(paymentId, clientIp));
            await db.Save(history);
        }

        #region Stripe Helpers
        public async Task<string> CreateCustomer(User user,
            RequestOptions requestOptions)
        {
            var createOptions = new CustomerCreateOptions
            {
                Description = "MorphicLite Customer",
                Email = user.Email.PlainText,
                Metadata = StripeMetadata(user),
            };
            var service = new CustomerService();
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

        public string CreateCard(History history, CreditCardInfo ccInfo, RequestOptions requestOptions)
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
                Metadata = StripeMetadata(history),
            };
            var service = new CardService();
            var response = service.Create(history.ProcessorCustomerId, options, requestOptions);
            return response.Id;
        }
        
        public string MakePayment(User user, History history, string source, long amount, string currency,
            RequestOptions requestOptions)
        {
            var options = new ChargeCreateOptions
            {
                Customer = history.ProcessorCustomerId,
                Amount = amount,
                Currency = currency,
                Source = source,
                Description = "My First Test Charge (created for API docs)",
                ReceiptEmail = user.Email.PlainText,
                Metadata = StripeMetadata(user),
            };
            var service = new ChargeService();
            var response = service.Create(options, requestOptions);
            return response.Id;
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