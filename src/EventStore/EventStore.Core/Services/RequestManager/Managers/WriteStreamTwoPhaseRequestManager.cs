// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.Services.Storage.ReaderIndex;

namespace EventStore.Core.Services.RequestManager.Managers
{
    public class WriteStreamTwoPhaseRequestManager : TwoPhaseRequestManagerBase, 
                                                     IHandle<ClientMessage.WriteEvents>
    {
        private ClientMessage.WriteEvents _request;

        public WriteStreamTwoPhaseRequestManager(IPublisher publisher, 
                                                 int prepareCount, 
                                                 int commitCount,
                                                 TimeSpan prepareTimeout,
                                                 TimeSpan commitTimeout)
                : base(publisher, prepareCount, commitCount, prepareTimeout, commitTimeout)
        {
        }

        public void Handle(ClientMessage.WriteEvents request)
        {
            _request = request;
            InitNoPreparePhase(request.Envelope, request.InternalCorrId, request.CorrelationId,
                               request.EventStreamId, request.User, StreamAccessType.Write);
        }

        protected override void OnSecurityAccessGranted(Guid internalCorrId)
        {
            Publisher.Publish(
                new StorageMessage.WritePrepares(
                    internalCorrId, PublishEnvelope, _request.EventStreamId, _request.ExpectedVersion, _request.Events,
                    liveUntil: NextTimeoutTime - TimeoutOffset));
            _request = null;
        }

        protected override void CompleteSuccessRequest(int firstEventNumber, int lastEventNumber)
        {
            base.CompleteSuccessRequest(firstEventNumber, lastEventNumber);
            ResponseEnvelope.ReplyWith(new ClientMessage.WriteEventsCompleted(ClientCorrId, firstEventNumber, lastEventNumber));
        }

        protected override void CompleteFailedRequest(OperationResult result, string error)
        {
            base.CompleteFailedRequest(result, error);
            ResponseEnvelope.ReplyWith(new ClientMessage.WriteEventsCompleted(ClientCorrId, result, error));
        }

    }
}