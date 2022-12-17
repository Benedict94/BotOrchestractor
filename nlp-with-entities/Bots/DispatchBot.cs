﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    public class DispatchBot : ActivityHandler
    {
        private ILogger<DispatchBot> _logger;
        private IBotServices _botServices;

        public DispatchBot(IBotServices botServices, ILogger<DispatchBot> logger)
        {
            _logger = logger;
            _botServices = botServices;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
           var dc = new DialogContext(new DialogSet(), turnContext, new DialogState());
            // Top intent tell us which cognitive service to use.
            var allScores = await _botServices.Dispatch.RecognizeAsync(dc, (Activity)turnContext.Activity, cancellationToken);
            var topIntent = allScores.Intents.First().Key;

            // Detected entities which could be used to help with intent dispatching, for example, when top intent score is too low or
            // when there are multiple top intents with close scores.
            var entities = allScores.Entities.Children().Where(t => t.Path != "$instance");
            
            // Next, we call the dispatcher with the top intent.
            await DispatchToTopIntentAsync(turnContext, topIntent, cancellationToken);
            //await DispatchToTopIntentAsync(turnContext, "HR", cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            const string WelcomeText = "Type a greeting, or a question about the weather to get started.";

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome to Dispatch bot {member.Name}. {WelcomeText}"), cancellationToken);
                }
            }
        }

        private async Task DispatchToTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string intent, CancellationToken cancellationToken)
        {
            switch (intent)
            {
                case "HRChildBot":
                    await ProcessHRAutomationAsync(turnContext, cancellationToken);
                    break;
                case "KB":
                    await ProcessKBAsync(turnContext, cancellationToken);
                    break;
               // case "QnAMaker":
                   // await ProcessSampleQnAAsync(turnContext, cancellationToken);
                   // break;
                default:
                    _logger.LogInformation($"Dispatch unrecognized intent: {intent}.");
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Dispatch unrecognized intent: {intent}."), cancellationToken);
                    break;
            }
        }

        private async Task ProcessHRAutomationAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessHRAutomationAsync");

            // Retrieve LUIS result for HomeAutomation.
            var recognizerResult = await _botServices.LuisHRRecognizer.RecognizeAsync(turnContext, cancellationToken);
            var result = recognizerResult.Properties["luisResult"] as LuisResult;

            var topIntent = result.TopScoringIntent.Intent; 
            
            await turnContext.SendActivityAsync(MessageFactory.Text($"HRAutomation top intent {topIntent}."), cancellationToken);
           // await turnContext.SendActivityAsync(MessageFactory.Text($"HRAutomation intents detected:\n\n{string.Join("\n\n", result.Intents.Select(i => i.Intent))}"), cancellationToken);
            if (result.Entities.Count > 0)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"HRAutomation entities were found in the message:\n\n{string.Join("\n\n", result.Entities.Select(i => i.Entity))}"), cancellationToken);
            }
        }

        private async Task ProcessKBAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessKBAsync");

            // Retrieve LUIS result for Weather.
            var recognizerResult = await _botServices.LuisWeatherRecognizer.RecognizeAsync(turnContext, cancellationToken);
            var result = recognizerResult.Properties["luisResult"] as LuisResult;
            var topIntent = result.TopScoringIntent.Intent;

            await turnContext.SendActivityAsync(MessageFactory.Text($"ProcessKB top intent {topIntent}."), cancellationToken);
            await turnContext.SendActivityAsync(MessageFactory.Text($"ProcessKB Intents detected::\n\n{string.Join("\n\n", result.Intents.Select(i => i.Intent))}"), cancellationToken);
            if (result.Entities.Count > 0)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"ProcessKB entities were found in the message:\n\n{string.Join("\n\n", result.Entities.Select(i => i.Entity))}"), cancellationToken);
            }
        }

      /*  private async Task ProcessSampleQnAAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessSampleQnAAsync");

            var results = await _botServices.SampleQnA.GetAnswersAsync(turnContext);
            if (results.Any())
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, could not find an answer in the Q and A system."), cancellationToken);
            }
        }
      */
    }
}