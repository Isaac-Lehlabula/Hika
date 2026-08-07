import '../../../core/networking/api_client.dart';
import 'user_profile.dart';

class ProfileApi {
  ProfileApi(this._client);

  final ApiClient _client;

  Future<UserProfile> getOwnProfile() async {
    final body = await _client.get('/api/v1/users/me');
    return UserProfile.fromJson(body!);
  }

  Future<UserProfile> updateProfile({required String firstName, required String lastName, String? photoUrl}) async {
    final body = await _client.put(
      '/api/v1/users/me',
      data: {'firstName': firstName, 'lastName': lastName, if (photoUrl != null) 'photoUrl': photoUrl},
    );
    return UserProfile.fromJson(body!);
  }
}
