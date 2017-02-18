﻿using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecipeShelf.Common.Proxies
{
    public interface IDistributedQueueProxy
    {
        Task ProcessMessagesAsync(string queueName, Func<List<Message>, Task> messagesProcessorAsync);
    }

}
