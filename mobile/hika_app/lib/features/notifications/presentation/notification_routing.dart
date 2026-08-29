/// Where tapping a notification of [type] (about [relatedEntityId]) should navigate — shared
/// between InboxScreen's in-app tap handling and PushService's FCM-tap handling so the mapping
/// (mirroring backend NotificationType) only lives once. Null means "nothing to navigate to".
({String path, Object? extra})? notificationRoute({
  required String type,
  required String? relatedEntityId,
  required String? currentUserId,
}) {
  if (relatedEntityId == null) {
    return null;
  }

  switch (type) {
    case 'BookingRequested' || 'BookingAccepted' || 'BookingDeclined' || 'PaymentSucceeded':
      return (path: '/bookings/$relatedEntityId', extra: null);
    case 'RideAlertMatched':
      return (path: '/trips/$relatedEntityId', extra: null);
    case 'NewReview':
      return currentUserId != null ? (path: '/users/$currentUserId/reviews', extra: 'Your') : null;
    default:
      return null;
  }
}
