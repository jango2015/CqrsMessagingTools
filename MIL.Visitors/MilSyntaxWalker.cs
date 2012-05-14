﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace MIL.Visitors
{
    public class MilSyntaxWalker : SyntaxWalker
    {
        public IList<ClassDeclarationSyntax> Commands { get; private set; }
        public IList<ClassDeclarationSyntax> Events { get; private set; }

        public IEnumerable<ClassDeclarationSyntax> EventHandlers { get { return EventToEventHandlersMapping.Values.SelectMany(x => x).AsEnumerable(); } }
        protected IDictionary<GenericNameSyntax, List<ClassDeclarationSyntax>> EventToEventHandlersMapping { get; set; }

        public IEnumerable<ClassDeclarationSyntax> CommandHandlers { get { return CommandHandlersWithCommands.Keys.AsEnumerable(); } }
        public IDictionary<ClassDeclarationSyntax, List<GenericNameSyntax>> CommandHandlersWithCommands { get; private set; }

        public List<MemberAccessExpressionSyntax> PublicationCalls { get; private set; }

        public MilSyntaxWalker() : base(visitIntoStructuredTrivia: true)
        {
            
            Commands = new List<ClassDeclarationSyntax>();
            Events = new List<ClassDeclarationSyntax>();
            CommandHandlersWithCommands = new Dictionary<ClassDeclarationSyntax, List<GenericNameSyntax>>();
            PublicationCalls = new List<MemberAccessExpressionSyntax>();
            EventToEventHandlersMapping = new Dictionary<GenericNameSyntax, List<ClassDeclarationSyntax>>();
        }

        protected const string CommandIfx = "ICommand";
        protected const string PublishKeyword = "Send";
        protected const string EventIfx = "IEvent";
        protected const string EventHandlerPlainIfx = "IEventHandler";
        protected const string CommandHandlerPlainIfx = "ICommandHandler";

        private readonly HandlerDeclarationSyntaxVisitor _cmdHandlerDeclarationVisitor = new HandlerDeclarationSyntaxVisitor(CommandHandlerPlainIfx);
        private readonly HandlerDeclarationSyntaxVisitor _eventHandlerDeclarationVisitor = new HandlerDeclarationSyntaxVisitor(EventHandlerPlainIfx);

        protected override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Name.GetText() == PublishKeyword)
            {
                PublicationCalls.Add(node);
            }
        }


        protected override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            LookForCommands(node);
            LookForCommandHandlers(node);
            LookForEvents(node);
            LookForEventHandlers(node);

            var methods = node.DescendentNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods.Where(x => x.BodyOpt != null))
            {
                Visit(method);
            }
        }

        private bool LookForEvents(ClassDeclarationSyntax node)
        {
            if (node.BaseListOpt != null && node.BaseListOpt.Types.Any(t => t.PlainName == EventIfx))
            {
                Events.Add(node);
                return true;
            }
            return false;
        }

        private bool LookForCommands(ClassDeclarationSyntax node)
        {
            if (node.BaseListOpt != null && node.BaseListOpt.Types.Any(t => t.PlainName == CommandIfx))
            {
                Commands.Add(node);
                return true;
            }
            return false;
        }

        private bool LookForEventHandlers(ClassDeclarationSyntax node)
        {
            var handles = _eventHandlerDeclarationVisitor.Visit(node);
            if (handles.Any())
            {
                foreach (var eventTypeName in handles)
                {
                    if (EventToEventHandlersMapping.ContainsKey(eventTypeName))
                    {
                        EventToEventHandlersMapping[eventTypeName].Add(node);
                    }
                    else
                    {
                        EventToEventHandlersMapping.Add(eventTypeName, new List<ClassDeclarationSyntax>() { node });
                    }
                }
                return true;
            }
            return false;
        }

        private bool LookForCommandHandlers(ClassDeclarationSyntax node)
        {
            var handles = _cmdHandlerDeclarationVisitor.Visit(node);
            if (handles.Any())
            {
                CommandHandlersWithCommands.Add(node, handles.ToList());
                return true;
            }
            return false;
        }
    }

    

  
}
