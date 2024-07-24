using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using GPTAssistant;

var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("OPENAI_API_KEY environment variable not set.");

var assistantClient = new AssistantsClient(openAIApiKey);
var assistantOption = new AssistantCreationOptions("gpt-3.5-turbo")
{
    Description = "You are a helpful AI assistant",
    Name = "GPTAssistant",
    Tools = { new CodeInterpreterToolDefinition() },
};

var assistant = new GPTAssistantAgent(assistantClient, assistantOption)
    .RegisterPrintMessage();


// start conversation
var task = """
    run the following code and tell me the output literally.
    
    ```python
    # Python program to find the 100th prime number
    # Function to generate N prime numbers
    def Nth_prime(N):
        i, count = 2, 0
        while True:
            prime = True
            for j in range(2, i//2 + 1):
                if i % j == 0:
                    prime = False
                    break
            if prime:
                count += 1
            if count == N:
                return i
            i += 1


    # Driver code
    N = 100
    # print the date today
    print(datetime.datetime.now())
    print(Nth_prime(N))
    print(f'The {N}th prime number is {Nth_prime(N)}')
    ```

    Put the output in a code block: ```output ``` and send it back to me.
    """;

await assistant.SendAsync(task);
