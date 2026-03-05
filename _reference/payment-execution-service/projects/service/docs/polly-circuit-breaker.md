## Polly circuit breaker

The circuit-breaker pattern has been implemented using Polly pipelines. To avoid tripping the circuit on internal exceptions, or on any exceptions which may be circumstantial and unrelated to the health of the underlying dependencies of this service, a predicate has been added.

This predicate will filter on the following:
* HttpRequestException and TimeoutException, to detect issues relating to the availability of other APIs (e.g. Stripe execution); and
* NpgsqlException, most crucially with IsTransient=true.

We do not want to trip the circuit and cut off application-wide traffic for just any SQL exception - many may be thrown due to issues which relate to the data of only one user or one payment. The IsTransient flag indicates that the SQL exception is either connection-related, or that the query or command will otherwise succeed if retried later; making it a suitable predicate for the detection of issues pertaining to the database itself.

For more context, see:
https://xero.atlassian.net/wiki/spaces/XFS/pages/270922514675/Proposal+concerning+circuit+breaker+patterns
