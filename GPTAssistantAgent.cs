using AutoGen.Core;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPTAssistant;

public class GPTAssistantAgent : IAgent
{
    private readonly AssistantsClient _client;
    private readonly Assistant _assistant;

    public GPTAssistantAgent(AssistantsClient client, AssistantCreationOptions option)
    {
        this.Name = option.Name;
        this._client = client;
        this._assistant = _client.CreateAssistant(option);
    }

    public string Name { get; }

    public async Task<IMessage> GenerateReplyAsync(IEnumerable<IMessage> messages, GenerateReplyOptions? options = null, CancellationToken cancellationToken = default)
    {
        // step 1
        // create thread
        var threadResponse = await _client.CreateThreadAsync(cancellationToken);
        var thread = threadResponse.Value;

        // step 2
        // add messages to thread
        // support textMessage only
        if (messages.Any(m => m is not TextMessage))
        {
            throw new ArgumentException("Only TextMessage is supported");
        }

        ThreadMessage? lastMessage = null;
        foreach (var message in messages)
        {
            var textMessage = (TextMessage)message;
            lastMessage = await _client.CreateMessageAsync(
                threadId: thread.Id,
                role: textMessage.From == this.Name ? MessageRole.Assistant : MessageRole.User,
                content: textMessage.Content,
                cancellationToken: cancellationToken);
        }

        // step 3
        // create run
        var runResponse = await _client.CreateRunAsync(
            threadId: thread.Id,
            createRunOptions: new CreateRunOptions(_assistant.Id),
            cancellationToken: cancellationToken);

        var run = runResponse.Value;

        // step 4
        // wait run complete
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            runResponse = await _client.GetRunAsync(thread.Id, run.Id);
        }
        while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);

        // step 5
        // get all new messages
        var newMessages = await _client.GetMessagesAsync(
            threadId: thread.Id,
            order: ListSortOrder.Ascending,
            after: lastMessage!.Id,
            cancellationToken: cancellationToken);

        // step 6
        // aggregate messages into one single message
        // we only collect text message
        var sb = new StringBuilder();
        foreach (var newMessage in newMessages.Value)
        {
            foreach (var part in newMessage.ContentItems)
            {
                if (part is MessageTextContent textItem)
                {
                    sb.AppendLine(textItem.Text);
                }
            }
        }

        // step 7
        // delete thread
        await _client.DeleteThreadAsync(thread.Id, cancellationToken);

        // step 8
        // return the result
        return new TextMessage(Role.Assistant, sb.ToString(), from: this.Name);
    }
}
